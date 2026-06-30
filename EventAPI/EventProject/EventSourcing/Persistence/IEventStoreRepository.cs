using EventAPI.Domain.Events;

namespace EventAPI.EventSourcing.Persistence
{
    public interface IEventStoreRepository
    {
        Task AppendEventsAsync(
            int aggregateId,
            IReadOnlyCollection<DomainEvent> events,
            CancellationToken cancellationToken = default);

        Task<List<DomainEvent>> GetEventsAsync(
            int aggregateId,
            int fromVersion = 0,
            CancellationToken cancellationToken = default);

        Task<int> GetCurrentVersionAsync(
            int aggregateId,
            CancellationToken cancellationToken = default);

        Task<List<EventStoreEntry>> GetHistoryAsync(
            int aggregateId,
            CancellationToken cancellationToken = default);
    }
}
