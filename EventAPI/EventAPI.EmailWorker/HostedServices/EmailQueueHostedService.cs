// HostedServices/EmailConsumerHostedService.cs
using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventAPI.EmailWorker.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.HostedServices;

public class EmailQueueConsumerHostedService : BackgroundService
{
    private const string EmailQueue = RoutingKeys.EmailQueue;
    private const int MaxEmailsPerMinute = 10;

    private readonly RabbitMqOptions _options;
    private readonly ILogger<EmailQueueConsumerHostedService> _logger;

    private readonly Queue<DateTime> _sentTimestamps = new();
    private readonly object _rateLock = new();

    private IConnection? _connection;
    private IChannel? _channel;

    public EmailQueueConsumerHostedService(
        IOptions<RabbitMqOptions> options,
        ILogger<EmailQueueConsumerHostedService> logger)
    {
        _options = options.Value;
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

        await _channel.QueueDeclareAsync(EmailQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);


        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var email = JsonSerializer.Deserialize<EmailMessage>(json);

            if (email is null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            await WaitForRateLimitSlotAsync(stoppingToken);

            try
            {
                await SaveEmailToFileAsync(email, stoppingToken);
                _logger.LogInformation("Email sačuvan: To={To}, Subject={Subject}", email.To, email.Subject);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri čuvanju emaila");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(EmailQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Email consumer pokrenut (limit: {Max} mejlova/min)", MaxEmailsPerMinute);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task WaitForRateLimitSlotAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TimeSpan? waitTime = null;

            lock (_rateLock)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddMinutes(-1);

                while (_sentTimestamps.Count > 0 && _sentTimestamps.Peek() < cutoff)
                    _sentTimestamps.Dequeue();

                if (_sentTimestamps.Count < MaxEmailsPerMinute)
                {
                    _sentTimestamps.Enqueue(now);
                    _logger.LogDebug("Rate limit: {Count}/{Max} mejlova u poslednjih 60s", _sentTimestamps.Count, MaxEmailsPerMinute);
                    return;
                }

                var waitUntil = _sentTimestamps.Peek().AddMinutes(1).AddMilliseconds(100);
                waitTime = waitUntil - now;

                _logger.LogInformation(
                    "Rate limit dostignut ({Max}/min). Čekam {Wait:F1}s",
                    MaxEmailsPerMinute, waitTime.Value.TotalSeconds);
            }

            if (waitTime.HasValue && waitTime.Value > TimeSpan.Zero)
                await Task.Delay(waitTime.Value, ct);
        }
    }

    private async Task SaveEmailToFileAsync(EmailMessage email, CancellationToken ct)
    {
        var outboxDir = Path.Combine(AppContext.BaseDirectory, "outbox");
        Directory.CreateDirectory(outboxDir);

        var guid = Guid.NewGuid().ToString("N")[..8];

        var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{guid}.txt";

        var filePath = Path.Combine(outboxDir, fileName);

        var content = $"""
            To: {email.To}
            Subject: {email.Subject}
            Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            Requested: {email.EnqueuedAt:yyyy-MM-dd HH:mm:ss} UTC
            
            {email.Body}
            """;

        await File.WriteAllTextAsync(filePath, content, ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel is not null) await _channel.CloseAsync(ct);
        if (_connection is not null) await _connection.CloseAsync(ct);
        await base.StopAsync(ct);
    }
}