namespace EventAPI.DTO.Messaging.Saga
{
    public class EventCreatedSagaReply : SagaMessage
    {
        public bool Success { get; set; }

        public int? EventId { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
