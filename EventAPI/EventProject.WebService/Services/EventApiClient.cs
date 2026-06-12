using EventAPI.WebPlatformService.Patterns;
using EventProject.DTO.DTOs;

namespace EventProject.WebService.Services
{
    public class EventApiClient : IEventApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EventApiClient> _logger;
        private readonly CircuitBreaker _circuitBreaker;

        public EventApiClient(HttpClient httpClient, ILogger<EventApiClient> logger, CircuitBreaker circuitBreaker)
        {
            _httpClient = httpClient;
            _logger = logger;
            _circuitBreaker = circuitBreaker;
        }


        public async Task<List<EventDetailsDto>> GetAllEventsAsync()
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _httpClient
                    .GetFromJsonAsync<List<EventDetailsDto>>("api/events");

                return result ?? new List<EventDetailsDto>();
            });
        }

        public async Task<EventDetailsDto> GetEventByIdAsync(int id)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _httpClient
                    .GetFromJsonAsync<EventDetailsDto>($"api/events/{id}");

                return result
                    ?? throw new Exception($"Event with id {id} not found.");
            });
        }

        public async Task<EventDetailsDto> CreateEventAsync(CreateEventDto createEventDto)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/events",
                        createEventDto);

                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<EventDetailsDto>();

                return result
                    ?? throw new Exception(
                        "Failed to create event (empty response).");
            });
        }

        public async Task<bool> UpdateEventAsync(int id, UpdateEventDto updateEventDto)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response = await _httpClient.PutAsJsonAsync($"api/events/{id}", updateEventDto);

                return response.IsSuccessStatusCode;
            });
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response =
                    await _httpClient.DeleteAsync($"api/events/{id}");

                return response.IsSuccessStatusCode;
            });
        }


        public async Task<List<EventLectureDto>> GetEventLecturesByEventIdAsync(int eventId)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _httpClient.GetFromJsonAsync<List<EventLectureDto>>
                    ($"api/event-lectures/by-event/{eventId}");

                return result ?? new List<EventLectureDto>();
            });
        }

        public async Task<EventLectureDto> CreateEventLectureAsync(CreateEventLectureDto createEventLectureDto)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/event-lectures",
                    createEventLectureDto);

                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<EventLectureDto>();

                return result ?? throw new Exception(
                    "Failed to create event lecture (empty response).");
            });
        }

        public async Task<bool> DeleteEventLectureAsync(int eventLectureId)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    $"api/event-lectures/{eventLectureId}");

                return response.IsSuccessStatusCode;
            });
        }
    }
}