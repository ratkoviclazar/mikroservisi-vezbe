using EventProject.DTO.DTOs;

namespace EventAPI.WebPlatformService.Services
{
    public interface IReferenceApiClient
    {
        Task<List<LocationDto>> GetAllLocationsAsync();
        Task<LocationDto> GetLocationByIdAsync(int id);
        Task<LocationDto> CreateLocationAsync(LocationDto createLocationDto);
        Task<LocationDto> UpdateLocationAsync(int id, LocationDto updateLocationDto);
        Task<bool> DeleteLocationAsync(int id);
        Task<List<EventTypeDto>> GetAllEventTypesAsync();
        Task<EventTypeDto> GetEventTypeByIdAsync(int id);
        Task<EventTypeDto> CreateEventTypeAsync(EventTypeDto createEventTypeDto);
        Task<EventTypeDto> UpdateEventTypeAsync(int id, EventTypeDto updateEventTypeDto);
        Task<bool> DeleteEventTypeAsync(int id);
    }
}
