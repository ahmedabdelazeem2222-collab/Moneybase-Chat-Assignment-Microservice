using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyBase.Support.Application.DTOs;
using MoneyBase.Support.Shared;
using System;
using System.Text.Json;

namespace MoneyBase.Support.Infrastructure.HostedServices
{
    public class QueueMonitorHostedService : BackgroundService, IDisposable
    {
        #region Fields
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueueMonitorHostedService> _logger;
        private readonly IGenericHttpClient _genericHttpClient;
        public QueueMonitorHostedService(ILogger<QueueMonitorHostedService> logger, IServiceScopeFactory scopeFactory,
            IGenericHttpClient genericHttpClient)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _genericHttpClient = genericHttpClient;
        }
        #endregion

        #region Job Execution
        /// <summary>
        /// The QueueMonitor background service runs continuously (check every 1s): 
        /// Detects inactive chats(no polls for > 3 seconds). 
        /// Marks those sessions as Inactive.
        /// Frees the agent’s slot so they can take a new chat.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Load active chat sessions from the Chat API
                    var sessionsResponse = await _genericHttpClient.GetAsync<IEnumerable<ChatSessionDto>>("/chats/active");
                    if (!sessionsResponse.Success || sessionsResponse.Data == null)
                    {
                        _logger.LogWarning("Failed to load active chat sessions from API");
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    var sessions = sessionsResponse.Data;

                    foreach (var session in sessions)
                    {
                        var secondsSincePoll = (now - session.LastPollAtUtc).TotalSeconds;
                        if (secondsSincePoll > 3)
                        {
                            _logger.LogInformation($"Update chat session: {session.Id}. session before {JsonSerializer.Serialize(session)}");

                            // Mark inactive and free agent slot
                            session.ChatStatus = Domain.Enums.ChatStatusEnum.InActive;

                            // Update session via API
                            var updateSessionResponse = await _genericHttpClient.PutAsync<ChatSessionDto, ChatSessionDto>($"/update-chat/{session.Id}/", session);
                            if (!updateSessionResponse.Success)
                            {
                                _logger.LogWarning($"Failed to update chat session {session.Id} via API");
                                continue;
                            }

                            _logger.LogInformation($"Update chat session: {session.Id} after marking inactive");

                            if (!string.IsNullOrEmpty(session.AgentId.ToString()))
                            {
                                // Load agent from API
                                var agentResponse = await _genericHttpClient.GetAsync<AgentDto>($"/get-agent/{session.AgentId}");
                                if (agentResponse.Success && agentResponse.Data != null)
                                {
                                    var agent = agentResponse.Data;
                                    agent.AssignedChatIds.ToList().Remove(session.Id.ToString());

                                    // Update agent via API
                                    var updateAgentResponse = await _genericHttpClient.PostAsync<AgentDto, AgentDto>($"/update-agent/{agent.Id}", agent);
                                    if (updateAgentResponse.Success)
                                    {
                                        _logger.LogInformation($"Removed chat session {session.Id} from agent {agent.Id} and freed agent slots");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Failed to update agent {agent.Id} via API");
                                    }
                                }
                            }

                            _logger.LogInformation($"Session: {session.Id} marked inactive after {secondsSincePoll} seconds");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "QueueMonitorHostedService/ExecuteAsync Error in queue monitor");
                }

                await Task.Delay(1000, stoppingToken); // check every 1s
            }
        }

        #endregion
    }
}
