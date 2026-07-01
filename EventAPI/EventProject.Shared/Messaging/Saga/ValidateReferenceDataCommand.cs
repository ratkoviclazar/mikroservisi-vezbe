namespace EventAPI.DTO.Messaging.Saga
{
    public class ValidateReferenceDataCommand : SagaMessage
    {
        public int LocationId { get; set; }

        public int EventTypeId { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
