using EventAPI.Data;
using EventAPI.Domain.Events;
using EventAPI.EventSourcing.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.EventSourcing.Persistence
{
    public sealed class EventStoreRepository : IEventStoreRepository
    {
        private readonly EventsDbContext _db;

        public EventStoreRepository(EventsDbContext db)
        {
            _db = db;
        }

        public async Task AppendEventsAsync(
            int aggregateId,
            IReadOnlyCollection<DomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            if (events.Count == 0)
                return;

            foreach (var domainEvent in events.OrderBy(x => x.Version))
            {
                var entry = new EventStoreEntry
                {
                    AggregateId = aggregateId,
                    AggregateType = "Event",
                    Version = domainEvent.Version,
                    EventType = domainEvent.GetType().Name,
                    EventData = EventSerializer.Serialize(domainEvent),
                    OccurredAt = domainEvent.OccurredAt
                };

                _db.EventStoreEntries.Add(entry);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<DomainEvent>> GetEventsAsync(
            int aggregateId,
            int fromVersion = 0,
            CancellationToken cancellationToken = default)
        {
            var entries = await _db.EventStoreEntries
                .Where(x => x.AggregateId == aggregateId && x.Version > fromVersion)
                .OrderBy(x => x.Version)
                .ToListAsync(cancellationToken);

            return entries
                .Select(x => EventSerializer.Deserialize(x.EventType, x.EventData))
                .ToList();
        }

        public async Task<int> GetCurrentVersionAsync(
            int aggregateId,
            CancellationToken cancellationToken = default)
        {
            return await _db.EventStoreEntries
                .Where(x => x.AggregateId == aggregateId)
                .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0;
        }

        public async Task<List<EventStoreEntry>> GetHistoryAsync(
            int aggregateId,
            CancellationToken cancellationToken = default)
        {
            return await _db.EventStoreEntries
                .Where(x => x.AggregateId == aggregateId)
                .OrderBy(x => x.Version)
                .ToListAsync(cancellationToken);
        }
    }
}
