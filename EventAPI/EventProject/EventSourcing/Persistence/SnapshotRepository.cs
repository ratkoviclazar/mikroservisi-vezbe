using EventAPI.Data;
using EventAPI.EventSourcing.Aggregates;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventAPI.EventSourcing.Persistence
{
    public sealed class SnapshotRepository : ISnapshotRepository
    {
        private readonly EventsDbContext _db;

        public SnapshotRepository(EventsDbContext db)
        {
            _db = db;
        }

        public async Task<EventSnapshot?> GetLatestSnapshotAsync(int aggregateId, CancellationToken ct = default)
        {
            return await _db.EventSnapshots
                .Where(x => x.AggregateId == aggregateId)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(ct);
        }

        public async Task SaveSnapshotAsync(EventAggregate aggregate, CancellationToken ct = default)
        {
            var snapshot = new EventSnapshot
            {
                AggregateId = aggregate.Id,
                Version = aggregate.Version,
                State = JsonSerializer.Serialize(aggregate.ToSnapshotState()),
                CreatedAt = DateTime.UtcNow
            };

            _db.EventSnapshots.Add(snapshot);

            await _db.SaveChangesAsync(ct);
        }
    }
}
