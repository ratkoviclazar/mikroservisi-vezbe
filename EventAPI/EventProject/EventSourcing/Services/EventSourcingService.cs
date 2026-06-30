using EventAPI.Domain.Events;
using EventAPI.DTO.DTOs;
using EventAPI.EventSourcing.Aggregates;
using EventAPI.EventSourcing.Persistence;
using System.Text.Json;

namespace EventAPI.EventSourcing.Services
{
    public sealed class EventSourcingService : IEventSourcingService
    {
        private const int SnapshotFrequency = 5;

        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly ISnapshotRepository _snapshotRepository;
        public EventSourcingService(
            IEventStoreRepository eventStoreRepository,
            ISnapshotRepository snapshotRepository)
        {
            _eventStoreRepository = eventStoreRepository;
            _snapshotRepository = snapshotRepository;
        }



        public async Task<EventAggregate?> GetByIdAsync(int eventId, CancellationToken ct = default)
        {
            var latestSnapshot = await _snapshotRepository.GetLatestSnapshotAsync(eventId, ct);

            EventAggregate aggregate;
            int fromVersion;

            if (latestSnapshot is not null)
            {
                var state = JsonSerializer.Deserialize<EventAggregateSnapshotState>(latestSnapshot.State);

                if (state is null)
                    throw new InvalidOperationException("Snapshot nije moguće deserijalizovati.");

                aggregate = EventAggregate.FromSnapshotState(state);
                fromVersion = latestSnapshot.Version;
            }
            else
            {
                aggregate = new EventAggregate();
                fromVersion = 0;
            }

            var events = await _eventStoreRepository.GetEventsAsync(eventId, fromVersion, ct);

            if (latestSnapshot is null && events.Count == 0)
                return null;

            foreach (var domainEvent in events.OrderBy(x => x.Version))
            {
                aggregate.ApplyFromHistory(domainEvent);
            }

            return aggregate;
        }

        public async Task SaveAsync(EventAggregate aggregate, CancellationToken ct = default)
        {
            if (!aggregate.UncommittedEvents.Any())
                return;

            await _eventStoreRepository.AppendEventsAsync(
                aggregate.Id,
                aggregate.UncommittedEvents,
                ct);

            if (aggregate.Version % SnapshotFrequency == 0)
            {
                await _snapshotRepository.SaveSnapshotAsync(aggregate, ct);
            }

            aggregate.ClearUncommittedEvents();
        }

        public async Task<List<DomainEvent>> GetHistoryAsync(int eventId, CancellationToken ct = default)
        {
            return await _eventStoreRepository.GetEventsAsync(eventId, 0, ct);
        }

        public async Task<List<EventHistoryItemDto>> GetHistoryViewAsync(int eventId, CancellationToken ct = default)
        {
            var history = await _eventStoreRepository.GetHistoryAsync(eventId, ct);

            return history.Select(x => new EventHistoryItemDto
            {
                Version = x.Version,
                EventType = x.EventType,
                EventData = x.EventData,
                OccurredAt = x.OccurredAt
            }).ToList();
        }
    }
}
