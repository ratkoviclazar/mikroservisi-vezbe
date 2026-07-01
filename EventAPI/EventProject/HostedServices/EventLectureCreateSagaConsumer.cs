using EventAPI.Data;
using EventAPI.Domains;
using EventAPI.DTO.Messaging.Saga;
using EventAPI.DTO.Shared;
using EventAPI.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.HostedServices
{
    public class EventLectureCreateSagaConsumer : BackgroundService
    {
        private readonly SagaRabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventLectureCreateSagaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public EventLectureCreateSagaConsumer(
            IOptions<SagaRabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<EventLectureCreateSagaConsumer> logger)
        {
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var exchange = _options.Exchange;
            var queue = RoutingKeys.SagaEventLectureCreateRequestQueue;
            var routingKey = RoutingKeys.SagaEventLectureCreateRequestQueue;

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
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: queue,
                exchange: exchange,
                routingKey: routingKey,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: RoutingKeys.SagaReplyQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: RoutingKeys.SagaReplyQueue,
                exchange: exchange,
                routingKey: RoutingKeys.SagaReplyQueue,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                CreateEventLectureSagaCommand? command = null;

                try
                {
                    command = JsonSerializer.Deserialize<CreateEventLectureSagaCommand>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (command is null)
                        throw new InvalidOperationException("CreateEventLectureSagaCommand nije validan.");

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                    var eventExists = db.Events.Any(x => x.Id == command.EventId);

                    if (!eventExists)
                        throw new InvalidOperationException($"Događaj sa ID={command.EventId} ne postoji.");

                    var eventLecture = new EventLecture
                    {
                        EventId = command.EventId,
                        LecturerId = command.LecturerId,
                        DateTime = command.DateTime,
                        DurationInHours = command.DurationInHours
                    };

                    db.EventLectures.Add(eventLecture);

                    await db.SaveChangesAsync(stoppingToken);

                    var reply = new EventLectureCreatedSagaReply
                    {
                        SagaId = command.SagaId,
                        CorrelationId = command.CorrelationId,
                        Success = true,
                        EventLectureId = eventLecture.Id,
                        ErrorMessage = null
                    };

                    await PublishReplyAsync(
                        reply,
                        exchange,
                        command.ReplyTo,
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Saga event lecture created. SagaId={SagaId}, EventLectureId={EventLectureId}, EventId={EventId}, LecturerId={LecturerId}",
                        command.SagaId,
                        eventLecture.Id,
                        command.EventId,
                        command.LecturerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade Saga create event lecture komande. Body: {Body}",
                        json);

                    if (command is not null)
                    {
                        try
                        {
                            var reply = new EventLectureCreatedSagaReply
                            {
                                SagaId = command.SagaId,
                                CorrelationId = command.CorrelationId,
                                Success = false,
                                EventLectureId = null,
                                ErrorMessage = ex.Message
                            };

                            await PublishReplyAsync(
                                reply,
                                exchange,
                                command.ReplyTo,
                                stoppingToken);
                        }
                        catch (Exception replyEx)
                        {
                            _logger.LogError(
                                replyEx,
                                "Greška prilikom slanja EventLectureCreatedSagaReply neuspeha. SagaId={SagaId}",
                                command.SagaId);
                        }
                    }

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "EventLectureCreateSagaConsumer started. Queue={Queue}, Exchange={Exchange}",
                queue,
                exchange);
        }

        private async Task PublishReplyAsync(
            EventLectureCreatedSagaReply reply,
            string exchange,
            string? replyTo,
            CancellationToken ct)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel nije inicijalizovan.");

            var routingKey = string.IsNullOrWhiteSpace(replyTo)
                ? RoutingKeys.SagaReplyQueue
                : replyTo;

            var json = JsonSerializer.Serialize(reply);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = nameof(EventLectureCreatedSagaReply),
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = reply.CorrelationId.ToString()
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
