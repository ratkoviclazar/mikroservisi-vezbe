using EventAPI.Domains;

namespace EventAPI.CQRS.DataAccess
{
    /// <summary>
    /// Pristup podacima namijenjen isključivo komandama (write strana).
    /// Query handleri nemaju pristup ovom interfejsu.
    /// </summary>
    public interface IEventsWriteStore
    {
        Task<bool> LocationExistsAsync(int locationId, CancellationToken cancellationToken = default);

        Task<bool> EventTypeExistsAsync(int typeId, CancellationToken cancellationToken = default);

        Task<int> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default);

        Task<bool> UpdateEventAsync(int id, Action<Event> applyChanges, CancellationToken cancellationToken = default);

        Task<bool> DeleteEventAsync(int id, CancellationToken cancellationToken = default);
    }
}
