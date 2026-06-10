using EventProject.LecturerService.Models;

namespace EventProject.DTO.DTOs
{
    public class EventLectureDto
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public EventDetailsDto? Event { get; set; }
        public int EventId { get; set; }
        public LecturerDto? Lecturer { get; set; }
        public int LecturerId { get; set; }
    }
}
