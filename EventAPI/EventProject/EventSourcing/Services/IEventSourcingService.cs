using EventAPI.Domain.Events;
using EventAPI.DTO.DTOs;
using EventAPI.EventSourcing.Aggregates;

namespace EventAPI.EventSourcing.Services
{
    public interface IEventSourcingService
    {
        Task<EventAggregate?> GetByIdAsync(int eventId, CancellationToken ct = default);

        Task SaveAsync(EventAggregate aggregate, CancellationToken ct = default);

        Task<List<DomainEvent>> GetHistoryAsync(int eventId, CancellationToken ct = default);

        Task<List<EventHistoryItemDto>> GetHistoryViewAsync(int eventId, CancellationToken ct = default);
    }
}
