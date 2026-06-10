# RabbitMQ Implementation Guide

## ?? Overview

Ovaj dokument opisuje kako implementirati RabbitMQ za event-driven komunikaciju izme?u mikroservisa.

## ??? Current State

### In-Memory Event Bus (Development)
- Lokacija: `EventProject.Infrastructure/Messaging/InMemoryEventBus.cs`
- Koristi se u dev okruženju
- Svi eventi se ?uvaju u memoriji

### RabbitMQ Template (Production)
- Lokacija: `EventProject.Infrastructure/Messaging/RabbitMQ/RabbitMQEventBus.cs`
- Sadrži TODO sekcije za implementaciju
- Spreman za zamenu In-Memory bus-a

## ?? Instalacija RabbitMQ

### Opcija 1: Docker (Preporu?eno)
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3.12-management
```

### Opcija 2: Windows
```bash
# Preuzmi sa https://www.rabbitmq.com/download.html
# Instaliraj RabbitMQ sa Erlang
```

## ?? Connection String

```csharp
// appsettings.json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

## ?? Implementacija koraka po koraka

### Korak 1: Instalacija NuGet paketa

U svakom servisu koji koristi RabbitMQ:

```powershell
dotnet add package RabbitMQ.Client --version 6.4.0
```

### Korak 2: Implementacija RabbitMQEventBusPublisher

Zameni TODO u `RabbitMQEventBus.cs`:

```csharp
public class RabbitMQEventBusPublisher : IEventBusPublisher
{
    private readonly ILogger<RabbitMQEventBusPublisher> _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly RabbitMQSettings _settings;

    public RabbitMQEventBusPublisher(
        ILogger<RabbitMQEventBusPublisher> logger,
        IOptions<RabbitMQSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        InitializeConnection();
    }

    private void InitializeConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _logger.LogInformation("RabbitMQ connection established");
    }

    public async Task PublishAsync<T>(string eventName, T eventData) where T : class
    {
        try
        {
            _channel.ExchangeDeclare(exchange: eventName, type: ExchangeType.Fanout);

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: eventName,
                routingKey: "",
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"Event published: {eventName}");
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

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

### Korak 3: Implementacija RabbitMQEventBusSubscriber

```csharp
public class RabbitMQEventBusSubscriber : IEventBusSubscriber
{
    private readonly ILogger<RabbitMQEventBusSubscriber> _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly RabbitMQSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, List<Delegate>> _subscribers = new();

    public RabbitMQEventBusSubscriber(
        ILogger<RabbitMQEventBusSubscriber> logger,
        IOptions<RabbitMQSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    public void Subscribe<T>(string eventName, Func<T, Task> handler) where T : class
    {
        if (!_subscribers.ContainsKey(eventName))
        {
            _subscribers[eventName] = new List<Delegate>();
            SetupQueue(eventName);
        }

        _subscribers[eventName].Add(handler);
        _logger.LogInformation($"Subscribed to event: {eventName}");
    }

    private void SetupQueue(string eventName)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: eventName, type: ExchangeType.Fanout);

        var queueName = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(queue: queueName, exchange: eventName, routingKey: "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await HandleMessage(ea, eventName);
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea, string eventName)
    {
        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation($"Message received from {eventName}: {message}");

            if (_subscribers.ContainsKey(eventName))
            {
                foreach (var handler in _subscribers[eventName])
                {
                    await (Task)handler.DynamicInvoke(
                        JsonSerializer.Deserialize(message, typeof(object))
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing message: {ex.Message}");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RabbitMQ Event Bus Subscriber started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RabbitMQ Event Bus Subscriber stopping...");
        _channel?.Dispose();
        _connection?.Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

### Korak 4: RabbitMQ Settings Klasa

Kreiraj `RabbitMQSettings.cs`:

```csharp
namespace EventProject.Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
    }
}
```

### Korak 5: DI Konfiguracija

Ažuriraj `MessagingServiceExtensions.cs`:

```csharp
public static class MessagingServiceExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMQSettings>(
            configuration.GetSection("RabbitMQ"));

        services.AddSingleton<IEventBusPublisher, RabbitMQEventBusPublisher>();
        services.AddSingleton<IEventBusSubscriber, RabbitMQEventBusSubscriber>();

        return services;
    }
}
```

### Korak 6: Program.cs Konfiguracija

U Event servisu:

```csharp
using EventProject.Infrastructure.Extensions;
using EventProject.Infrastructure.Messaging.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddInMemoryEventBus();
}
// Production
else
{
    builder.Services.AddRabbitMQEventBus(builder.Configuration);
}

// Ostala konfiguracija...
```

## ?? Event Publishing Primjer

```csharp
public class EventService : IEventService
{
    private readonly IEventBusPublisher _eventBusPublisher;

    public EventService(
        EventDbContext context,
        IEventBusPublisher eventBusPublisher,
        ILogger<EventService> logger)
    {
        _context = context;
        _eventBusPublisher = eventBusPublisher;
        _logger = logger;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto)
    {
        var eventEntity = new Event { /* ... */ };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Objavi event
        var integrationEvent = new EventCreatedIntegrationEvent
        {
            EventId = eventEntity.Id,
            Name = eventEntity.Name,
            Price = eventEntity.Price,
            DateTime = eventEntity.DateTime,
            LocationId = eventEntity.LocationId,
            TypeId = eventEntity.TypeId
        };

        await _eventBusPublisher.PublishAsync(
            nameof(EventCreatedIntegrationEvent),
            integrationEvent);

        return MapToDto(eventEntity);
    }
}
```

## ?? Event Handling Primjer

```csharp
public class EventLectureEventHandler
{
    private readonly IEventBusSubscriber _eventBusSubscriber;
    private readonly ILogger<EventLectureEventHandler> _logger;

    public EventLectureEventHandler(
        IEventBusSubscriber eventBusSubscriber,
        ILogger<EventLectureEventHandler> logger)
    {
        _eventBusSubscriber = eventBusSubscriber;
        _logger = logger;

        Subscribe();
    }

    private void Subscribe()
    {
        _eventBusSubscriber.Subscribe<EventCreatedIntegrationEvent>(
            nameof(EventCreatedIntegrationEvent),
            HandleEventCreated);

        _eventBusSubscriber.Subscribe<EventDeletedIntegrationEvent>(
            nameof(EventDeletedIntegrationEvent),
            HandleEventDeleted);
    }

    private async Task HandleEventCreated(EventCreatedIntegrationEvent @event)
    {
        _logger.LogInformation($"Event created: {@event.Name}");
        // Obradi event...
        await Task.CompletedTask;
    }

    private async Task HandleEventDeleted(EventDeletedIntegrationEvent @event)
    {
        _logger.LogInformation($"Event deleted: {@event.EventId}");
        // Obradi event...
        await Task.CompletedTask;
    }
}
```

## ?? RabbitMQ Monitoring

Otvori Management portal:
```
http://localhost:15672
```

Login:
- Username: `guest`
- Password: `guest`

## ? Testing RabbitMQ

Kreiraj unit testove:

```csharp
[TestFixture]
public class RabbitMQPublisherTests
{
    private RabbitMQEventBusPublisher _publisher;
    private Mock<ILogger<RabbitMQEventBusPublisher>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<RabbitMQEventBusPublisher>>();
        _publisher = new RabbitMQEventBusPublisher(_loggerMock.Object, options);
    }

    [Test]
    public async Task PublishAsync_WithValidEvent_ShouldPublish()
    {
        // Arrange
        var @event = new EventCreatedIntegrationEvent
        {
            EventId = 1,
            Name = "Test Event"
        };

        // Act
        await _publisher.PublishAsync(nameof(EventCreatedIntegrationEvent), @event);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ));
    }
}
```

## ?? Production Checklist

- [ ] Testovao sam RabbitMQ na lokalnoj mašini
- [ ] Pokrenuo sam sve tri servise
- [ ] Proverio sam RabbitMQ Management portal
- [ ] Events se publikuju i primaju
- [ ] Hendleri se pravilno pokrenuli
- [ ] Logging je pravilno konfigurisan
- [ ] Dodao sam error handling
- [ ] Testovao sam failover scenarije
- [ ] Konfigurirao sam queue persistence
- [ ] Postavio sam monitoring

## ?? Support

Ako naideš na probleme:
1. Proveri RabbitMQ logs: `docker logs rabbitmq`
2. Vidi Management portal na `localhost:15672`
3. Proveri application logs
4. Verifikuj connection string u `appsettings.json`

