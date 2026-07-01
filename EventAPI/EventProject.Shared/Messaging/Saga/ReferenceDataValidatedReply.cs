namespace EventAPI.DTO.Messaging.Saga
{
    public class ReferenceDataValidatedReply : SagaMessage
    {
        public bool Success { get; set; }

        public bool LocationExists { get; set; }

        public bool EventTypeExists { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
