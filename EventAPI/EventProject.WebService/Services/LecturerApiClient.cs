using EventProject.LecturerService.Models;

namespace EventAPI.WebPlatformService.Services
{
    public class LecturerApiClient : ILecturerApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LecturerApiClient> _logger;

        public LecturerApiClient(HttpClient httpClient, ILogger<LecturerApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<LecturerDto>> GetAllLecturersAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<LecturerDto>>("api/lecturers");
            return result ?? new List<LecturerDto>();
        }

        public async Task<LecturerDto> GetLecturerByIdAsync(int id)
        {
            var result = await _httpClient.GetFromJsonAsync<LecturerDto>($"api/lecturers/{id}");

            return result ?? throw new Exception($"Lecturer {id} not found.");
        }

        public async Task<LecturerDto> CreateLecturerAsync(LecturerDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/lecturers", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LecturerDto>();
            return result ?? throw new Exception("Failed to create lecturer.");
        }

        public async Task<LecturerDto> UpdateLecturerAsync(int id, LecturerDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/lecturers/{id}", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LecturerDto>();
            return result ?? throw new Exception("Failed to update lecturer.");
        }

        public async Task<bool> DeleteLecturerAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/lecturers/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}