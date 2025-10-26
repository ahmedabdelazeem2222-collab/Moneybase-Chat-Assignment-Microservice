using Microsoft.Extensions.Logging;
using MoneyBase.Support.Application.DTOs;
using MoneyBase.Support.Application.Interfaces;
using MoneyBase.Support.Application.Services;
using MoneyBase.Support.Domain.Enums;
using MoneyBase.Support.Shared;

public class AssignChatHandler
{
    #region Fields
    private readonly IChatAgentProducer _chatAgentProducer;
    private readonly ILogger<AssignChatHandler> _logger;
    private const int OverflowCount = 6;
    private const SeniorityEnum OverflowSeniority = SeniorityEnum.Junior;
    private readonly IGenericHttpClient _httpClient;
    private readonly ShiftService _shiftService;
    public AssignChatHandler(
        IChatAgentProducer chatAgentProducer, ILogger<AssignChatHandler> logger, IGenericHttpClient httpClient,
        ShiftService shiftService)
    {
        _chatAgentProducer = chatAgentProducer;
        _logger = logger;
        _httpClient = httpClient;
        _shiftService = shiftService;
    }
    #endregion

    #region Handler
    public async Task HandleAsync(ChatRequestDto dto, CancellationToken ct = default)
    {
        try
        {
            #region get chat and create If not found in DB (edge case) create initial
            var chatResponse = await _httpClient.GetAsync<ChatSessionDto>($"/get-chat/{dto.ChatId}");
            if (!chatResponse.Success)
            {
                _logger.LogError("Failed to load chat {ChatId}: {Message}", dto.ChatId, chatResponse.Message);
                return;
            }

            var chat = chatResponse.Data;
            if (chat == null)
            {
                // If not found in DB (edge case) create initial
              await CreateChat();
            }
            #endregion

            #region Compute capacity and queue limits
            var agents = await LoadAllAgents();

            // Compute capacity and queue limits
            var capacity = _shiftService.CalculateAgentCapacity(agents);
            var maxQueue = (int)Math.Floor(capacity * 1.5);

            // calling GetPendingChatCountAsync API in Chat API microservice
            var pendingCount = await GetPendingChatCountAsync();

            // If queue full and not office hours -> refuse
            if (pendingCount >= maxQueue)
            {
                if (!_shiftService.IsOfficeHours())
                {
                    chat.ChatStatus = ChatStatusEnum.Refused;
                    // calling update chat API in Chat API microservice
                    await UpdateChatAsync(new ChatSessionDto() { Id = chat.Id, ChatStatus = chat.ChatStatus, UserId = chat.UserId });
                    _logger.LogInformation("AssignChatHandler/HandleAsync Chat {Chat} refused — queue full & outside office hours", chat.Id);
                    return;
                }

                // during office hours, add overflow team if not already present
                await AddOverflowTeam(agents, capacity, maxQueue);
                
                // if even after overflow it's still full -> refuse
                pendingCount = await GetPendingChatCountAsync();
                if (pendingCount >= maxQueue)
                {
                    chat.ChatStatus = ChatStatusEnum.Refused;
                    await UpdateChatAsync(new ChatSessionDto() { Id = chat.Id, ChatStatus = chat.ChatStatus, UserId = chat.UserId });
                    _logger.LogInformation("AssignChatHandler/HandleAsync Chat {Chat} refused — queue + overflow full", chat.Id);
                    return;
                }
            }

            #endregion

            #region Round robin: iterate candidates and assign it until we find one below capacity

            // Try assign: prioritize junior -> mid -> senior -> teamlead
            var now = _shiftService.GetCairoNow();
            var candidates = agents
                .Where(a => a.IsOnShift(now))
                .OrderBy(a => a.Seniority) //junior first
                .ThenBy(a => a.AssignedChatIds?.Count() ?? 0)
                .ToList();
            foreach (var agent in candidates)
            {
                var assignedCount = agent.AssignedChatIds?.Count() ?? 0;
                if (assignedCount < agent.MaxConcurrency)
                {
                    // call assign chat to agent API in Chat API
                    await AssignChatAsync(chat.Id, agent.Id);

                    // push the assigned chat to the agent queue
                    PushChatToAgentQueue(agent.Id, chat.UserId, chat.Id, ct);

                    _logger.LogInformation("AssignChatHandler/HandleAsync Assigned chat {Chat} to {Agent}", chat.Id, agent.Name);
                    return;
                }
            }

            #endregion

            // no agent available -> keep it pending; if desired, could requeue, or mark refused
            // Here we keep it pending to be retried later by the worker
            _logger.LogInformation("AssignChatHandler/HandleAsync No agent free now for {Chat}, remains pending", chat.Id);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "AssignChatHandler/HandleAsync Error");
        }
    }

    #endregion

    #region Private Methods
    /// <summary>
    /// Push Chat To Agent Queue
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="userId"></param>
    /// <param name="chatId"></param>
    /// <param name="ct"></param>
    private async void PushChatToAgentQueue(Guid agentId, string userId, Guid chatId, CancellationToken ct)
    {
        var message = new ChatAssignedMessage
        {
            ChatId = chatId,
            AgentId = agentId,
            UserId = userId,
            Channel = ChannelEnum.Web.ToString(),
            AssignedAt = DateTime.UtcNow
        };

        await _chatAgentProducer.PublishAsync(message, ct);
    }
    private async Task<ChatSessionDto> CreateChatAsync(ChatSessionDto chat)
    {
        var response = await _httpClient.PostAsync<ChatSessionDto, ChatSessionDto>("/add-chat", chat);
        if (!response.Success)
        {
            _logger.LogError("Failed to create chat {ChatId}: {Message}",null, response.Message);
            return null;
        }

        return response.Data;
    }
    private async Task<IEnumerable<AgentDto>> LoadAllAgents()
    {
        var agentsResponse = await _httpClient.GetAsync<IEnumerable<AgentDto>>("/get-all-agents");

        if (!agentsResponse.Success)
        {
            _logger.LogError("Failed to load agents: {Message}", agentsResponse.Message);
            return null;
        }

        return agentsResponse.Data;
    }
    private async Task<int> GetPendingChatCountAsync()
    {
        var response = await _httpClient.GetAsync<int>("/pending");
        if (!response.Success)
        {
            _logger.LogWarning("Failed to get pending chat count: {Message}", response.Message);
            return 0;
        }

        return response.Data;
    }
    private async Task<bool> AssignChatAsync(Guid chatId, Guid agentId)
    {
        var payload = new { ChatId = chatId, AgentId = agentId };
        var response = await _httpClient.PostAsync<object, string>("/assign", payload);
        if (!response.Success)
        {
            _logger.LogError("Failed to assign chat {ChatId} to agent {AgentId}: {Message}", chatId, agentId, response.Message);
            return false;
        }

        _logger.LogInformation("Chat {ChatId} assigned to agent {AgentId}", chatId, agentId);
        return true;
    }
    private async Task<bool> UpdateChatAsync(ChatSessionDto chat)
    {
        chat.UpdatedDate = DateTime.UtcNow;
        chat.UpdatedBy = 0; //system 
        var response = await _httpClient.PutAsync<ChatSessionDto, ChatSessionDto>($"/update-chat/{chat.Id}", chat);
        if (!response.Success)
        {
            _logger.LogError("Failed to update chat {ChatId}: {Message}", chat.Id, response.Message);
            return false;
        }

        _logger.LogInformation("Chat {ChatId} updated successfully", chat.Id);
        return true;
    }
    private async Task<AgentDto> AddAgentAsync(AgentDto agentDto)
    {
        var response = await _httpClient.PostAsync<AgentDto, AgentDto>("/add-agent", agentDto);
        if (!response.Success)
        {
            _logger.LogError("Failed to add agent {AgentName}: {Message}", agentDto.Name, response.Message);
            return null;
        }
        _logger.LogInformation("Agent {AgentName} added successfully", agentDto.Name);
        return response.Data;
    }
    private async Task CreateChat()
    {
        ChatSessionDto chatSessionDto = new ChatSessionDto();
        chatSessionDto.ChatStatus = ChatStatusEnum.Pending;
        chatSessionDto.UserId = Guid.NewGuid().ToString();
        chatSessionDto.Id = Guid.NewGuid();
        chatSessionDto.CreatedDate = DateTime.UtcNow;
        await CreateChatAsync(chatSessionDto);
    }
    private async Task AddOverflowTeam(IEnumerable<AgentDto> agents, int capacity, int maxQueue)
    {
        if (!agents.Any(a => a.Name.StartsWith("Overflow-")))
        {
            for (int i = 0; i < OverflowCount; i++)
            {
                var overflowAgent = new
                {
                    Id = Guid.NewGuid(),
                    Name = $"Overflow-{i + 1}",
                    Seniority = OverflowSeniority,
                    ShiftStartHour = 9,
                    ShiftEndHour = 17,
                    IsOverflow = true
                };
                AgentDto agentDto = new AgentDto();
                agentDto.ShiftStartHour = overflowAgent.ShiftStartHour;
                agentDto.ShiftEndHour = overflowAgent.ShiftEndHour;
                agentDto.Id = overflowAgent.Id;
                agentDto.Name = overflowAgent.Name;
                agentDto.Seniority = overflowAgent.Seniority;
                agentDto.IsOverflow = overflowAgent.IsOverflow;
                agentDto.CreationDate = DateTime.UtcNow;
                agentDto.CreatedBy = 0;
                await AddAgentAsync(agentDto); // calling add agent API
                                               // in Chat API microservice
            }
            capacity = _shiftService.CalculateAgentCapacity(agents);
            maxQueue = (int)Math.Floor(capacity * 1.5);
        }
    }
    #endregion
}
