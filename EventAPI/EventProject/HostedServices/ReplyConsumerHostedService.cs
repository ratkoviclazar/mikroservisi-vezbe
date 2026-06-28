using EventAPI.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventAPI.HostedServices
{
    public class ReplyConsumerHostedService : BackgroundService
    {
        private readonly RequestReplyClient _requestReplyClient;
        private readonly ILogger<ReplyConsumerHostedService> _logger;

        public ReplyConsumerHostedService(RequestReplyClient requestReplyClient, ILogger<ReplyConsumerHostedService> logger)
        {
            _requestReplyClient = requestReplyClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000, stoppingToken);
            await _requestReplyClient.EnsureInitializedAsync(stoppingToken);

            var channel = _requestReplyClient.Channel!;

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var correlationId = Guid.TryParse(ea.BasicProperties.CorrelationId, out var g) ? g : Guid.Empty;

                _logger.LogDebug("Primljen reply za CorrelationId={CId}", correlationId);
                _requestReplyClient.Complete(correlationId, json);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            };

            await channel.BasicConsumeAsync(
                queue: RequestReplyClient.ReplyQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }
    }
}
