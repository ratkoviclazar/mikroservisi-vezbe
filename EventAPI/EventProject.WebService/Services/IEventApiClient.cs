using EventProject.DTO.DTOs;

namespace EventProject.WebService.Services
{
    public interface IEventApiClient
    {
        Task<List<EventDetailsDto>> GetAllEventsAsync();
        Task<EventDetailsDto> GetEventByIdAsync(int id);
        Task<List<EventDetailsDto>> SearchEventsAsync(
            string? name = null,
            int? locationId = null,
            int? typeId = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<EventDetailsDto> CreateEventAsync(CreateEventDto createEventDto);
        Task<bool> UpdateEventAsync(int id, UpdateEventDto updateEventDto);
        Task<bool> DeleteEventAsync(int id);
        Task<List<EventLectureDto>> GetEventLecturesByEventIdAsync(int eventLectureId);
        Task<EventLectureDto> CreateEventLectureAsync(CreateEventLectureDto createEventLectureDto);
        Task<bool> DeleteEventLectureAsync(int eventLectureId);
    }
}
