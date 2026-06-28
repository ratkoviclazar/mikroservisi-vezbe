using EventAPI.Data;
using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventAPI.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventAPI.HostedServices
{
    public class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcherHostedService> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
        private const int BatchSize = 20;

        public OutboxDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
                    var emailPublisher = scope.ServiceProvider.GetRequiredService<IEmailPublisher>();

                    var pending = await db.OutboxMessages
                        .Where(x => !x.IsProcessed &&
                            (!x.IsProcessing || x.CreatedAt < DateTime.UtcNow.AddMinutes(-2)))
                        .OrderBy(x => x.CreatedAt)
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (pending.Count == 0)
                    {
                        await Task.Delay(_pollInterval, stoppingToken);
                        continue;
                    }

                    foreach (var msg in pending) msg.IsProcessing = true;
                    await db.SaveChangesAsync(stoppingToken);

                    foreach (var msg in pending)
                    {
                        try
                        {
                            switch (msg.Type)
                            {
                                case RoutingKeys.EmailSent:
                                    var email = JsonSerializer.Deserialize<EmailMessage>(msg.Payload)!;
                                    await emailPublisher.PublishAsync(email, stoppingToken);
                                    break;

                                default:
                                    _logger.LogWarning("Nepoznat Outbox Type: {Type}", msg.Type);
                                    break;
                            }

                            msg.IsProcessed = true;
                            msg.ProcessedAtUtc = DateTime.UtcNow;
                            msg.IsProcessing = false;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Slanje poruke {MessageId} nije uspelo", msg.MessageId);
                            msg.IsProcessing = false;
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatcher error");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }
}
