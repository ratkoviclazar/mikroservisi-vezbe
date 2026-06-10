using EventProject.Shared.Messaging;
using System.Text.Json;

namespace EventProject.Infrastructure.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ implementacija Event Bus Publisher-a
    /// TODO: Implementirati RabbitMQ nakon što se DI postavi
    /// </summary>
    public class RabbitMQEventBusPublisher : IEventBusPublisher
    {
        private readonly ILogger<RabbitMQEventBusPublisher> _logger;
        // private IConnection _connection;
        // private IModel _channel;

        public RabbitMQEventBusPublisher(ILogger<RabbitMQEventBusPublisher> logger)
        {
            _logger = logger;
            // TODO: Inicijalizovati RabbitMQ konekciju
        }

        public async Task PublishAsync<T>(string eventName, T eventData) where T : class
        {
            try
            {
                _logger.LogInformation($"[RABBITMQ] Publishing event: {eventName}");
                _logger.LogInformation($"Event data: {JsonSerializer.Serialize(eventData)}");

                // TODO: Implementirati RabbitMQ publish logiku
                // var message = JsonSerializer.Serialize(eventData);
                // var body = Encoding.UTF8.GetBytes(message);
                // _channel.BasicPublish(exchange: eventName, routingKey: "", basicProperties: null, body: body);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error publishing event {eventName}: {ex.Message}");
                throw;
            }
        }

        public async Task PublishAsync(string eventName, object eventData, Type dataType)
        {
            await PublishAsync<dynamic>(eventName, eventData as dynamic);
        }
    }

    /// <summary>
    /// RabbitMQ implementacija Event Bus Subscriber-a
    /// TODO: Implementirati RabbitMQ nakon što se DI postavi
    /// </summary>
    public class RabbitMQEventBusSubscriber : IEventBusSubscriber
    {
        private readonly ILogger<RabbitMQEventBusSubscriber> _logger;
        // private IConnection _connection;
        // private IModel _channel;
        private Dictionary<string, List<Delegate>> _subscribers = new();

        public RabbitMQEventBusSubscriber(ILogger<RabbitMQEventBusSubscriber> logger)
        {
            _logger = logger;
            // TODO: Inicijalizovati RabbitMQ konekciju
        }

        public void Subscribe<T>(string eventName, Func<T, Task> handler) where T : class
        {
            if (!_subscribers.ContainsKey(eventName))
            {
                _subscribers[eventName] = new List<Delegate>();
            }

            _subscribers[eventName].Add(handler);
            _logger.LogInformation($"[RABBITMQ] Subscribed to event: {eventName}");

            // TODO: Implementirati RabbitMQ subscribe logiku
            // SetupQueue(eventName);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[RABBITMQ] Event Bus Subscriber starting...");
            // TODO: Implementirati RabbitMQ start logiku
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[RABBITMQ] Event Bus Subscriber stopping...");
            // TODO: Implementirati RabbitMQ stop logiku
            await Task.CompletedTask;
        }

        // TODO: Implementirati privatne metode za RabbitMQ
        // private void SetupQueue(string eventName) { }
        // private void HandleMessage(string message, string eventName) { }
    }
}
