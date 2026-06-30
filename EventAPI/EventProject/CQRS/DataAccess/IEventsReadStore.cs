using EventAPI.Domains;

namespace EventAPI.CQRS.DataAccess
{
    public interface IEventsReadStore
    {
        Task<List<Event>> GetAllWithLecturesAsync(CancellationToken cancellationToken = default);

        Task<Event?> GetByIdWithLecturesAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Event>> FilterAsync(
            string? nameContains,
            int? locationId,
            int? typeId,
            System.DateTime? fromDate,
            System.DateTime? toDate,
            CancellationToken cancellationToken = default);

        Task<LocationSnapshot?> GetLocationSnapshotAsync(int externalLocationId, CancellationToken cancellationToken = default);

        Task<EventTypeSnapshot?> GetEventTypeSnapshotAsync(int externalTypeId, CancellationToken cancellationToken = default);
    }
}
