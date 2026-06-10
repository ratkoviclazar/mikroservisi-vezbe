using EventProject.Shared.Messaging;
using System.Text.Json;

namespace EventProject.Infrastructure.Messaging
{
    /// <summary>
    /// In-Memory implementacija Event Bus-a (privremeno, kasnije ?e biti RabbitMQ)
    /// </summary>
    public class InMemoryEventBusPublisher : IEventBusPublisher
    {
        private readonly ILogger<InMemoryEventBusPublisher> _logger;

        public InMemoryEventBusPublisher(ILogger<InMemoryEventBusPublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync<T>(string eventName, T eventData) where T : class
        {
            _logger.LogInformation($"[IN-MEMORY] Publishing event: {eventName}");
            _logger.LogInformation($"Event data: {JsonSerializer.Serialize(eventData)}");
            await Task.CompletedTask;
        }

        public async Task PublishAsync(string eventName, object eventData, Type dataType)
        {
            _logger.LogInformation($"[IN-MEMORY] Publishing event: {eventName}");
            _logger.LogInformation($"Event data: {JsonSerializer.Serialize(eventData)}");
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// In-Memory implementacija Event Bus Subscriber-a (privremeno, kasnije ?e biti RabbitMQ)
    /// </summary>
    public class InMemoryEventBusSubscriber : IEventBusSubscriber
    {
        private readonly ILogger<InMemoryEventBusSubscriber> _logger;
        private Dictionary<string, List<Delegate>> _subscribers = new();

        public InMemoryEventBusSubscriber(ILogger<InMemoryEventBusSubscriber> logger)
        {
            _logger = logger;
        }

        public void Subscribe<T>(string eventName, Func<T, Task> handler) where T : class
        {
            if (!_subscribers.ContainsKey(eventName))
            {
                _subscribers[eventName] = new List<Delegate>();
            }

            _subscribers[eventName].Add(handler);
            _logger.LogInformation($"[IN-MEMORY] Subscribed to event: {eventName}");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[IN-MEMORY] Event Bus Subscriber started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[IN-MEMORY] Event Bus Subscriber stopped");
            await Task.CompletedTask;
        }
    }
}
