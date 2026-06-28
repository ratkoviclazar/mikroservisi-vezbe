namespace Catalog_Service.HostedServices
{
    using EventProject.CatalogService.Data;
    using global::Catalog_Service.Messaging;

    using Microsoft.EntityFrameworkCore;

    public class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcherHostedService> _logger;

        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
        private const int BatchSize = 20;

        public OutboxDispatcherHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxDispatcherHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Dispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                    var pending = await db.OutboxMessages
                        .Where(x => !x.IsProcessed && !x.IsProcessing)
                        .OrderBy(x => x.CreatedAt)
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (pending.Count == 0)
                    {
                        await Task.Delay(_pollInterval, stoppingToken);
                        continue;
                    }

                    foreach (var msg in pending)
                    {
                        msg.IsProcessing = true;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    foreach (var msg in pending)
                    {
                        try
                        {
                            await publisher.PublishAsync(
                                msg.MessageId,
                                msg.Type,
                                msg.Payload,
                                stoppingToken);

                            msg.IsProcessed = true;
                            msg.ProcessedAtUtc = DateTime.UtcNow;
                            msg.IsProcessing = false;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to publish message {MessageId}",
                                msg.MessageId);

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

            _logger.LogInformation("Outbox Dispatcher stopped.");
        }
    }
}
