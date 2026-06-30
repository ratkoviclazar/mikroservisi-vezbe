using EventAPI.EventSourcing.Aggregates;

namespace EventAPI.EventSourcing.Persistence
{
    public interface ISnapshotRepository
    {
        Task<EventSnapshot?> GetLatestSnapshotAsync(int aggregateId, CancellationToken ct = default);

        Task SaveSnapshotAsync(EventAggregate aggregate, CancellationToken ct = default);
    }
}
