using EventProject.Infrastructure.Messaging;
using EventProject.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EventProject.Infrastructure.Extensions
{
    /// <summary>
    /// Extension metode za registraciju messaging servisa
    /// </summary>
    public static class MessagingServiceExtensions
    {
        /// <summary>
        /// Registruje In-Memory event bus (za development)
        /// </summary>
        public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IEventBusPublisher, InMemoryEventBusPublisher>();
            services.AddSingleton<IEventBusSubscriber, InMemoryEventBusSubscriber>();
            return services;
        }

        /// <summary>
        /// Registruje RabbitMQ event bus (za production)
        /// </summary>
        public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, string connectionString)
        {
            // TODO: Implementirati RabbitMQ registraciju sa pravim connection stringom
            // services.AddSingleton<IEventBusPublisher>(sp => 
            //     new RabbitMQEventBusPublisher(sp.GetRequiredService<ILogger<RabbitMQEventBusPublisher>>(), connectionString));
            // services.AddSingleton<IEventBusSubscriber>(sp => 
            //     new RabbitMQEventBusSubscriber(sp.GetRequiredService<ILogger<RabbitMQEventBusSubscriber>>(), connectionString));

            // Za sada koristimo In-Memory kao fallback
            return services.AddInMemoryEventBus();
        }
    }
}
