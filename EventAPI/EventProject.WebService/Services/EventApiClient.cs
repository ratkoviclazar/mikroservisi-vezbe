using EventProject.DTO.DTOs;

namespace EventProject.WebService.Services
{
    public class EventApiClient : IEventApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EventApiClient> _logger;

        public EventApiClient(HttpClient httpClient, ILogger<EventApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<List<EventDetailsDto>> GetAllEventsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<EventDetailsDto>>("api/events");
            return result ?? new List<EventDetailsDto>();
        }

        public async Task<EventDetailsDto> GetEventByIdAsync(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<EventDetailsDto>($"api/events/{id}");

            return result ?? throw new Exception($"Event with id {id} not found.");
        }

        public async Task<EventDetailsDto> CreateEventAsync(CreateEventDto createEventDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/events", createEventDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EventDetailsDto>();

            return result ?? throw new Exception("Failed to create event (empty response).");
        }

        public async Task<bool> UpdateEventAsync(int id, UpdateEventDto updateEventDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/events/{id}", updateEventDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            return response.IsSuccessStatusCode;
        }


        public async Task<List<EventLectureDto>> GetEventLecturesByEventIdAsync(int eventId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<EventLectureDto>>
                ($"api/event-lectures/by-event/{eventId}");

            return result ?? new List<EventLectureDto>();
        }

        public async Task<EventLectureDto> CreateEventLectureAsync(CreateEventLectureDto createEventLectureDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/event-lectures", createEventLectureDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EventLectureDto>();

            return result ?? throw new Exception("Failed to create event lecture (empty response).");
        }

        public async Task<bool> DeleteEventLectureAsync(int eventLectureId)
        {
            var response = await _httpClient.DeleteAsync(
                $"api/event-lectures/{eventLectureId}"
            );

            return response.IsSuccessStatusCode;
        }
    }
}