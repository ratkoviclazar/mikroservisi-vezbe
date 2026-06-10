namespace EventProject.EventService.Models
{
    public class EventLecture
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int LecturerId { get; set; }
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public Event Event { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
