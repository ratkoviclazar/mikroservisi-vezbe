using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.Models;
using System.Text.Json;

namespace EventAPI.SagaOrchestratorService.Messaging
{
    public class SagaOutboxService : ISagaOutboxService
    {
        private readonly SagaDbContext _db;

        public SagaOutboxService(SagaDbContext db)
        {
            _db = db;
        }

        public Task AddAsync<TMessage>(
            Guid sagaId,
            string exchange,
            string routingKey,
            TMessage message,
            CancellationToken ct = default)
            where TMessage : class
        {
            var outboxMessage = new SagaOutboxMessage
            {
                MessageId = Guid.NewGuid(),
                SagaId = sagaId,
                Exchange = exchange,
                Type = typeof(TMessage).Name,
                RoutingKey = routingKey,
                Payload = JsonSerializer.Serialize(message),
                CreatedAtUtc = DateTime.UtcNow,
                IsPublished = false
            };

            _db.SagaOutboxMessages.Add(outboxMessage);

            return Task.CompletedTask;
        }
    }
}
