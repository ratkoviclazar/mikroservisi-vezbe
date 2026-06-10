namespace EventProject.DTO.DTOs
{
    public class CreateEventLectureDto
    {
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public int EventId { get; set; }
        public int LecturerId { get; set; }
    }

}
