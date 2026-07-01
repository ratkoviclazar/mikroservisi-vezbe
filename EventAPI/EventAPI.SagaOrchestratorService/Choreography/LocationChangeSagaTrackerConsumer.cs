using EventAPI.DTO.Messaging.Saga.Choreography;
using EventAPI.DTO.Shared;
using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.Models;
using EventAPI.SagaOrchestratorService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.SagaOrchestratorService.Choreography
{
    public class LocationChangeSagaTrackerConsumer : BackgroundService
    {
        private const string QueueName = "choreography.location.change.saga.tracker.queue";

        private readonly RabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LocationChangeSagaTrackerConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public LocationChangeSagaTrackerConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<LocationChangeSagaTrackerConsumer> logger)
        {
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: "reference.exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: "event.exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await BindReferenceExchangeAsync(stoppingToken);
            await BindEventExchangeAsync(stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var messageType = ea.BasicProperties.Type;

                try
                {
                    await HandleMessageAsync(messageType, json, stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška u LocationChangeSagaTrackerConsumer. MessageType={MessageType}, Body={Body}",
                        messageType,
                        json);

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "LocationChangeSagaTrackerConsumer started. Queue={Queue}",
                QueueName);
        }

        private async Task BindReferenceExchangeAsync(CancellationToken ct)
        {
            await _channel!.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationChangeRequested,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationReserved,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationReservationFailed,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationReservationCancelRequested,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationReservationCancelled,
                cancellationToken: ct);
        }

        private async Task BindEventExchangeAsync(CancellationToken ct)
        {
            await _channel!.QueueBindAsync(
                queue: QueueName,
                exchange: "event.exchange",
                routingKey: RoutingKeys.EventLocationChanged,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "event.exchange",
                routingKey: RoutingKeys.EventLocationChangeFailed,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "event.exchange",
                routingKey: RoutingKeys.LocationChangeNotificationSent,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "event.exchange",
                routingKey: RoutingKeys.LocationChangeNotificationFailed,
                cancellationToken: ct);
        }

        private async Task HandleMessageAsync(
            string? messageType,
            string json,
            CancellationToken ct)
        {
            switch (messageType)
            {
                case nameof(LocationChangeRequested):
                    await HandleLocationChangeRequestedAsync(json, ct);
                    break;

                case nameof(LocationReserved):
                    await HandleLocationReservedAsync(json, ct);
                    break;

                case nameof(LocationReservationFailed):
                    await HandleLocationReservationFailedAsync(json, ct);
                    break;

                case nameof(EventLocationChanged):
                    await HandleEventLocationChangedAsync(json, ct);
                    break;

                case nameof(EventLocationChangeFailed):
                    await HandleEventLocationChangeFailedAsync(json, ct);
                    break;

                case nameof(LocationReservationCancelRequested):
                    await HandleLocationReservationCancelRequestedAsync(json, ct);
                    break;

                case nameof(LocationReservationCancelled):
                    await HandleLocationReservationCancelledAsync(json, ct);
                    break;

                case nameof(LocationChangeNotificationSent):
                    await HandleLocationChangeNotificationSentAsync(json, ct);
                    break;

                case nameof(LocationChangeNotificationFailed):
                    await HandleLocationChangeNotificationFailedAsync(json, ct);
                    break;

                default:
                    _logger.LogWarning(
                        "Nepoznat choreography message type: {MessageType}",
                        messageType);
                    break;
            }
        }

        private async Task HandleLocationChangeRequestedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationChangeRequested>(json);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();

            var existing = await db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == message.SagaId, ct);

            if (existing is not null)
                return;

            var saga = new SagaState
            {
                Id = message.SagaId,
                SagaType = "ChangeEventLocationChoreographySaga",
                Status = SagaStatus.Started,
                CurrentStep = "LocationChangeRequested",

                EventId = message.EventId,
                LocationId = message.NewLocationId,
                EventName = message.EventName,
                EventDateTime = message.EventDateTime,

                StartedAtUtc = DateTime.UtcNow,
                Log = BuildLogLine(
                    $"Location change requested. EventId={message.EventId}, OldLocationId={message.OldLocationId}, NewLocationId={message.NewLocationId}.")
            };

            db.SagaStates.Add(saga);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Choreography saga tracked as started. SagaId={SagaId}",
                message.SagaId);
        }

        private async Task HandleLocationReservedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationReserved>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Started;
                    saga.CurrentStep = "LocationReserved";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.Log += BuildLogLine(
                        $"Location reserved. ReservationId={message.ReservationId}, NewLocationId={message.NewLocationId}.");
                },
                ct);
        }

        private async Task HandleLocationReservationFailedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationReservationFailed>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Failed;
                    saga.CurrentStep = "LocationReservationFailed";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.ErrorMessage = message.ErrorMessage;
                    saga.FailedAtUtc = DateTime.UtcNow;
                    saga.Log += BuildLogLine(
                        $"Location reservation failed. Error={message.ErrorMessage}");
                },
                ct);
        }

        private async Task HandleEventLocationChangedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<EventLocationChanged>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Started;
                    saga.CurrentStep = "EventLocationChanged";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.Log += BuildLogLine(
                        $"Event location changed. EventId={message.EventId}, OldLocationId={message.OldLocationId}, NewLocationId={message.NewLocationId}.");
                },
                ct);
        }

        private async Task HandleEventLocationChangeFailedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<EventLocationChangeFailed>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Compensating;
                    saga.CurrentStep = "EventLocationChangeFailed";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.ErrorMessage = message.ErrorMessage;
                    saga.Log += BuildLogLine(
                        $"Event location change failed. Compensation should start. Error={message.ErrorMessage}");
                },
                ct);
        }

        private async Task HandleLocationReservationCancelRequestedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationReservationCancelRequested>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Compensating;
                    saga.CurrentStep = "LocationReservationCancelRequested";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.ErrorMessage = message.Reason;
                    saga.Log += BuildLogLine(
                        $"Location reservation cancellation requested. ReservationId={message.ReservationId}, Reason={message.Reason}");
                },
                ct);
        }

        private async Task HandleLocationReservationCancelledAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationReservationCancelled>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Compensated;
                    saga.CurrentStep = "LocationReservationCancelled";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.CompensatedAtUtc = DateTime.UtcNow;
                    saga.Log += BuildLogLine(
                        $"Location reservation cancelled. ReservationId={message.ReservationId}. Saga compensated.");
                },
                ct);
        }

        private async Task HandleLocationChangeNotificationSentAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationChangeNotificationSent>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Completed;
                    saga.CurrentStep = "Completed";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.CompletedAtUtc = DateTime.UtcNow;
                    saga.Log += BuildLogLine(
                        $"Location change notification sent. Saga completed.");
                },
                ct);
        }

        private async Task HandleLocationChangeNotificationFailedAsync(string json, CancellationToken ct)
        {
            var message = Deserialize<LocationChangeNotificationFailed>(json);

            await UpdateSagaAsync(
                message.SagaId,
                saga =>
                {
                    saga.Status = SagaStatus.Failed;
                    saga.CurrentStep = "LocationChangeNotificationFailed";
                    saga.EventId = message.EventId;
                    saga.LocationId = message.NewLocationId;
                    saga.ErrorMessage = message.ErrorMessage;
                    saga.FailedAtUtc = DateTime.UtcNow;
                    saga.Log += BuildLogLine(
                        $"Location change notification failed. Error={message.ErrorMessage}");
                },
                ct);
        }

        private async Task UpdateSagaAsync(
            Guid sagaId,
            Action<SagaState> update,
            CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();

            var saga = await db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == sagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "SagaState not found for SagaId={SagaId}. Event ignored by tracker.",
                    sagaId);

                return;
            }

            update(saga);

            await db.SaveChangesAsync(ct);
        }

        private static T Deserialize<T>(string json)
        {
            var message = JsonSerializer.Deserialize<T>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (message is null)
                throw new InvalidOperationException($"Poruka tipa {typeof(T).Name} nije validna.");

            return message;
        }

        private static string BuildLogLine(string message)
        {
            return $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}";
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
                await _channel.DisposeAsync();

            if (_connection is not null)
                await _connection.DisposeAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}
