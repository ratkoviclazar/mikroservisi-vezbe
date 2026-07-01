namespace EventAPI.DTO.Messaging.Saga
{
    public class DeleteEventSagaCommand : SagaMessage
    {
        public int EventId { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
