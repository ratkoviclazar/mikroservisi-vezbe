namespace EventAPI.DTO.Messaging.Saga
{
    public class CreateEventSagaCommand : SagaMessage
    {
        public string Name { get; set; } = string.Empty;

        public string Agenda { get; set; } = string.Empty;

        public DateTime DateTime { get; set; }

        public decimal DurationInHours { get; set; }

        public decimal Price { get; set; }

        public int TypeId { get; set; }

        public int LocationId { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
