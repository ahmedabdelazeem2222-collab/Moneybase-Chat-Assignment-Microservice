using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using MoneyBase.Support.Application.DTOs;
using Microsoft.Extensions.Configuration;
using MoneyBase.Support.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyBase.Support.Infrastructure.HostedServices
{
    public class ChatAssignmentWorker : BackgroundService, IDisposable
    {
        #region Fields
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "chat_queue";
        private readonly ILogger<ChatAssignmentWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitSettings _rabbitSettings;
        public ChatAssignmentWorker(ILogger<ChatAssignmentWorker> logger, IServiceScopeFactory scopeFactory,
            IConfiguration configuration, IOptions<RabbitSettings> rabbitOptions)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _rabbitSettings = rabbitOptions.Value;
            var factory = new ConnectionFactory 
            { 
                HostName = _rabbitSettings.Host,
                UserName = _rabbitSettings.User,
                Password = _rabbitSettings.Pass,
                DispatchConsumersAsync = true 
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        }
        #endregion

        #region Job Execution
        /// <summary>
        /// The ChatAssignmentWorker processes chat messages from the queue.
        /// For each chat:
        /// - Calculates current capacity based on agents currently on shift.
        /// - If the queue is full:
        ///     • During office hours, attempts to add to overflow.
        ///     • Otherwise, marks the chat as Refused.
        /// - If capacity allows, assigns the chat to an available agent using round-robin order:
        ///     Junior → Mid → Senior → TeamLead.
        /// - Each agent can handle up to 10 × multiplier concurrent chats.
        /// -Push the assigned chat to the agent queue
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (s, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var dto = JsonSerializer.Deserialize<ChatRequestDto>(json);
                    _logger.LogInformation($"Message received from queue {_queueName}, message body: {json}");
                    
                    if (dto != null)
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            // Resolve the scoped service inside the scope
                            var assignChatHandler = scope.ServiceProvider.GetRequiredService<AssignChatHandler>();

                            // Call your handler logic
                            await assignChatHandler.HandleAsync(dto,stoppingToken);
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChatAssignmentWorker/ExecuteAsync Error handling message");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            try { _channel?.Close(); _connection?.Close(); } catch { }
        }
        #endregion
    }
}
