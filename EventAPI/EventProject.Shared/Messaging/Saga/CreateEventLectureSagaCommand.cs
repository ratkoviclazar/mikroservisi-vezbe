namespace EventAPI.DTO.Messaging.Saga
{
    public class CreateEventLectureSagaCommand : SagaMessage
    {
        public int EventId { get; set; }

        public int LecturerId { get; set; }

        public DateTime DateTime { get; set; }

        public decimal DurationInHours { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
