using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyBase.Support.Application.Interfaces;
using MoneyBase.Support.Application.Services;
using MoneyBase.Support.Infrastructure.Configuration;
using MoneyBase.Support.Infrastructure.HostedServices;
using MoneyBase.Support.Infrastructure.MessageBus;

namespace MoneyBase.Support.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMoneyBaseServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.Configure<RabbitSettings>(configuration.GetSection("Rabbit"));

            // Repositories

            // Application services
            services.AddScoped<AssignChatHandler>();
            services.AddScoped<ShiftService>();

            // RabbitMQ producer
            services.AddSingleton<IChatAgentProducer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RabbitSettings>>().Value;
                var logger = sp.GetRequiredService<ILogger<RabbitMqChatAgentProducer>>();
                return new RabbitMqChatAgentProducer(options.Host, options.User, options.Pass, logger);
            });


            return services;
        }
        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<ChatAssignmentWorker>();
            services.AddHostedService<QueueMonitorHostedService>();

            return services;
        }
    }
}
