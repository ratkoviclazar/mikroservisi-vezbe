using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.SagaOrchestratorService.HostedServices
{
    public class SagaOutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaOutboxDispatcherHostedService> _logger;

        public SagaOutboxDispatcherHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SagaOutboxDispatcherHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Saga Outbox Dispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<ISagaMessagePublisher>();

                    var messages = await db.SagaOutboxMessages
                        .Where(x => !x.IsPublished)
                        .OrderBy(x => x.CreatedAtUtc)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    foreach (var message in messages)
                    {
                        try
                        {
                            await publisher.PublishRawAsync(
                                message.Exchange,
                                message.Payload,
                                message.Type,
                                message.RoutingKey,
                                stoppingToken);

                            message.IsPublished = true;
                            message.PublishedAtUtc = DateTime.UtcNow;
                            message.ErrorMessage = null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to publish saga outbox message {MessageId}",
                                message.MessageId);

                            message.ErrorMessage = ex.Message;
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Saga outbox dispatcher iteration failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
