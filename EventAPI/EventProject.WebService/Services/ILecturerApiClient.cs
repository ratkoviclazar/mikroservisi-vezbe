using EventProject.LecturerService.Models;

namespace EventAPI.WebPlatformService.Services
{
    public interface ILecturerApiClient
    {
        Task<List<LecturerDto>> GetAllLecturersAsync();
        Task<LecturerDto> GetLecturerByIdAsync(int id);
        Task<LecturerDto> CreateLecturerAsync(LecturerDto createLecturerDto);
        Task<LecturerDto> UpdateLecturerAsync(int id, LecturerDto updateLecturerDto);
        Task<bool> DeleteLecturerAsync(int id);
    }
}
