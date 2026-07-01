using EventAPI.Data;
using EventAPI.DTO.Messaging.Saga.Choreography;
using EventAPI.DTO.Shared;
using EventAPI.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.HostedServices
{
    public class ChoreographyEventLocationChangeConsumer : BackgroundService
    {
        private const string QueueName = RoutingKeys.ChoreographyEventLocationChangeRequestQueue;

        private readonly SagaRabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChoreographyEventLocationChangeConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public ChoreographyEventLocationChangeConsumer(
            IOptions<SagaRabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<ChoreographyEventLocationChangeConsumer> logger)
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

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "reference.exchange",
                routingKey: RoutingKeys.LocationReserved,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                LocationReserved? message = null;

                try
                {
                    message = JsonSerializer.Deserialize<LocationReserved>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (message is null)
                        throw new InvalidOperationException("LocationReserved poruka nije validna.");

                    // Privremeni test za kompenzaciju.
                    // Ako događaj u nazivu ima FAIL, simulira se greška posle rezervacije lokacije.
                    if (message.EventName.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
                    {
                        await PublishFailureAndCompensationAsync(
                            message,
                            "Simulirana greška prilikom promene lokacije događaja.",
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                    var eventItem = await db.Events
                        .FirstOrDefaultAsync(x => x.Id == message.EventId, stoppingToken);

                    if (eventItem is null)
                    {
                        await PublishFailureAndCompensationAsync(
                            message,
                            $"Događaj sa ID={message.EventId} ne postoji.",
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    eventItem.LocationId = message.NewLocationId;

                    await db.SaveChangesAsync(stoppingToken);

                    var changed = new EventLocationChanged
                    {
                        SagaId = message.SagaId,
                        CorrelationId = message.CorrelationId,
                        EventId = message.EventId,
                        OldLocationId = message.OldLocationId,
                        NewLocationId = message.NewLocationId,
                        ReservationId = message.ReservationId,
                        EventName = message.EventName,
                        EventDateTime = message.EventDateTime
                    };

                    await PublishAsync(
                        changed,
                        "event.exchange",
                        RoutingKeys.EventLocationChanged,
                        nameof(EventLocationChanged),
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Event location changed. SagaId={SagaId}, EventId={EventId}, OldLocationId={OldLocationId}, NewLocationId={NewLocationId}",
                        message.SagaId,
                        message.EventId,
                        message.OldLocationId,
                        message.NewLocationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade LocationReserved poruke. Body: {Body}",
                        json);

                    /*
                     * Ako je message uspešno deserijalizovan, znači da je Catalog Service
                     * već prethodno rezervisao lokaciju.
                     *
                     * Zato ovde ne sme samo BasicNack, nego treba pokrenuti kompenzaciju:
                     * 1. EventLocationChangeFailed
                     * 2. LocationReservationCancelRequested
                     */
                    if (message is not null)
                    {
                        await PublishFailureAndCompensationAsync(
                            message,
                            ex.Message,
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    /*
                     * Ako poruka nije mogla ni da se pročita, nemaš SagaId,
                     * ReservationId itd, pa ne možeš pravilno da pokreneš kompenzaciju.
                     */
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
                "ChoreographyEventLocationChangeConsumer started. Queue={Queue}, InputExchange={InputExchange}, OutputExchange={OutputExchange}, RoutingKey={RoutingKey}",
                QueueName,
                "reference.exchange",
                "event.exchange",
                RoutingKeys.LocationReserved);
        }

        private async Task PublishFailureAndCompensationAsync(
            LocationReserved message,
            string errorMessage,
            CancellationToken ct)
        {
            var failed = new EventLocationChangeFailed
            {
                SagaId = message.SagaId,
                CorrelationId = message.CorrelationId,
                EventId = message.EventId,
                OldLocationId = message.OldLocationId,
                NewLocationId = message.NewLocationId,
                ReservationId = message.ReservationId,
                ErrorMessage = errorMessage
            };

            await PublishAsync(
                failed,
                "event.exchange",
                RoutingKeys.EventLocationChangeFailed,
                nameof(EventLocationChangeFailed),
                ct);

            var cancelRequested = new LocationReservationCancelRequested
            {
                SagaId = message.SagaId,
                CorrelationId = message.CorrelationId,
                EventId = message.EventId,
                ReservationId = message.ReservationId,
                NewLocationId = message.NewLocationId,
                Reason = errorMessage
            };

            await PublishAsync(
                cancelRequested,
                "reference.exchange",
                RoutingKeys.LocationReservationCancelRequested,
                nameof(LocationReservationCancelRequested),
                ct);

            _logger.LogWarning(
                "Event location change failed. Compensation requested. SagaId={SagaId}, EventId={EventId}, ReservationId={ReservationId}, Reason={Reason}",
                message.SagaId,
                message.EventId,
                message.ReservationId,
                errorMessage);
        }

        private async Task PublishAsync<T>(
            T message,
            string exchange,
            string routingKey,
            string messageType,
            CancellationToken ct)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel nije inicijalizovan.");

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = messageType,
                MessageId = Guid.NewGuid().ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
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
