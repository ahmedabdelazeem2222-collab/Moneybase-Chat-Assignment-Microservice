using Microsoft.Extensions.Logging;
using MoneyBase.Support.Application.DTOs;
using MoneyBase.Support.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MoneyBase.Support.Infrastructure.MessageBus
{
    public class RabbitMqChatAgentProducer : IChatAgentProducer, IDisposable
    {
        #region Fields
        private readonly IConnection _connection;
        private readonly RabbitMQ.Client.IModel _channel;
        private readonly ILogger<RabbitMqChatAgentProducer> _logger;
        private const string ExchangeName = "chat.agent.exchange";
        private const string ExchangeType = "topic";
        public RabbitMqChatAgentProducer(string host, string user, string pass, ILogger<RabbitMqChatAgentProducer> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
       
            // Declare exchange only once
            _channel.ExchangeDeclare(ExchangeName, ExchangeType, durable: true);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Push chat message the agent queue using Topic exchange which is pattern match as we push to the queue 
        /// related to the agent (each agent has its own queue)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task PublishAsync(ChatAssignedMessage message, CancellationToken ct = default)
        {
            try
            {
                var routingKey = $"agent.{message.AgentId}";
                var queueName = $"agent.{message.AgentId}.queue";

                // Ensure the agent queue exists and is bound
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

                _channel.QueueBind(queueName, ExchangeName, routingKey);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation($"Published chat {message.ChatId} to {queueName}");

                return Task.CompletedTask;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "RabbitMqChatProducer/PublishAsync Error.");
                return Task.FromException(ex);
            }
        }
        public void Dispose()
        {
            try { _channel?.Close(); _connection?.Close(); } catch { }
        }
        #endregion

    }
}
