using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Commands;
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
    public class EventCreateSagaConsumer : BackgroundService
    {
        private readonly SagaRabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventCreateSagaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public EventCreateSagaConsumer(
            IOptions<SagaRabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<EventCreateSagaConsumer> logger)
        {
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var exchange = _options.Exchange;
            var queue = RoutingKeys.SagaEventCreateRequestQueue;
            var routingKey = RoutingKeys.SagaEventCreateRequestQueue;

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

                CreateEventSagaCommand? sagaCommand = null;

                try
                {
                    sagaCommand = JsonSerializer.Deserialize<CreateEventSagaCommand>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (sagaCommand is null)
                        throw new InvalidOperationException("CreateEventSagaCommand nije validan.");

                    using var scope = _scopeFactory.CreateScope();

                    var handler = scope.ServiceProvider
                        .GetRequiredService<ICommandHandler<CreateEventCommand, CommandResult<int>>>();

                    var createCommand = new CreateEventCommand
                    {
                        Name = sagaCommand.Name,
                        Agenda = sagaCommand.Agenda,
                        DateTime = sagaCommand.DateTime,
                        DurationInHours = sagaCommand.DurationInHours,
                        Price = sagaCommand.Price,
                        TypeId = sagaCommand.TypeId,
                        LocationId = sagaCommand.LocationId
                    };

                    var result = await handler.HandleAsync(
                        createCommand,
                        stoppingToken);

                    if (result.Status != CommandStatus.Success)
                    {
                        var failedReply = new EventCreatedSagaReply
                        {
                            SagaId = sagaCommand.SagaId,
                            CorrelationId = sagaCommand.CorrelationId,
                            Success = false,
                            EventId = null,
                            ErrorMessage = BuildErrorMessage(result.Errors)
                        };

                        await PublishReplyAsync(
                            failedReply,
                            exchange,
                            sagaCommand.ReplyTo,
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    var reply = new EventCreatedSagaReply
                    {
                        SagaId = sagaCommand.SagaId,
                        CorrelationId = sagaCommand.CorrelationId,
                        Success = true,
                        EventId = result.Data,
                        ErrorMessage = null
                    };

                    await PublishReplyAsync(
                        reply,
                        exchange,
                        sagaCommand.ReplyTo,
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Saga event created through CQRS. SagaId={SagaId}, EventId={EventId}",
                        sagaCommand.SagaId,
                        result.Data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade Saga create event komande. Body: {Body}",
                        json);

                    if (sagaCommand is not null)
                    {
                        try
                        {
                            var reply = new EventCreatedSagaReply
                            {
                                SagaId = sagaCommand.SagaId,
                                CorrelationId = sagaCommand.CorrelationId,
                                Success = false,
                                EventId = null,
                                ErrorMessage = ex.Message
                            };

                            await PublishReplyAsync(
                                reply,
                                exchange,
                                sagaCommand.ReplyTo,
                                stoppingToken);
                        }
                        catch (Exception replyEx)
                        {
                            _logger.LogError(
                                replyEx,
                                "Greška prilikom slanja EventCreatedSagaReply neuspeha. SagaId={SagaId}",
                                sagaCommand.SagaId);
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
                "EventCreateSagaConsumer started. Queue={Queue}, Exchange={Exchange}",
                queue,
                exchange);
        }

        private async Task PublishReplyAsync(
            EventCreatedSagaReply reply,
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
                Type = nameof(EventCreatedSagaReply),
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

        private static string BuildErrorMessage(IReadOnlyList<string> errors)
        {
            return errors.Count == 0
                ? "Komanda nije uspešno izvršena."
                : string.Join("; ", errors);
        }
    }
}