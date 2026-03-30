namespace EventAPI.Domains
{
    public class EventLecture
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public int EventId { get; set; }
        public int LecturerId { get; set; }
        public Event? Event { get; set; }
        public Lecturer? Lecturer { get; set; }

    }
}
