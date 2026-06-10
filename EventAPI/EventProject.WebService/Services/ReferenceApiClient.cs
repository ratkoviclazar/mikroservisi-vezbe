using EventProject.DTO.DTOs;

namespace EventAPI.WebPlatformService.Services
{
    public class ReferenceApiClient : IReferenceApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReferenceApiClient> _logger;

        public ReferenceApiClient(HttpClient httpClient, ILogger<ReferenceApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<LocationDto>> GetAllLocationsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<LocationDto>>("api/locations");
            return result ?? new List<LocationDto>();
        }

        public async Task<LocationDto> GetLocationByIdAsync(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<LocationDto>($"api/locations/{id}");
            return result ?? throw new Exception($"Location {id} not found.");
        }

        public async Task<LocationDto> CreateLocationAsync(LocationDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/locations", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LocationDto>();
            return result ?? throw new Exception("Failed to create location.");
        }

        public async Task<LocationDto> UpdateLocationAsync(int id, LocationDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/locations/{id}", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LocationDto>();
            return result ?? throw new Exception("Failed to update location.");
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/locations/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<EventTypeDto>> GetAllEventTypesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<EventTypeDto>>("api/event-types");
            return result ?? new List<EventTypeDto>();
        }

        public async Task<EventTypeDto> GetEventTypeByIdAsync(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<EventTypeDto>($"api/event-types/{id}");
            return result ?? throw new Exception($"EventType {id} not found.");
        }

        public async Task<EventTypeDto> CreateEventTypeAsync(EventTypeDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/event-types", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EventTypeDto>();
            return result ?? throw new Exception("Failed to create event type.");
        }

        public async Task<EventTypeDto> UpdateEventTypeAsync(int id, EventTypeDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/event-types/{id}", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EventTypeDto>();
            return result ?? throw new Exception("Failed to update event type.");
        }

        public async Task<bool> DeleteEventTypeAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/event-types/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}