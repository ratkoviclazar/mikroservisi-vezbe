namespace EventAPI.DTO.Messaging.Saga
{
    public class LecturerValidatedReply : SagaMessage
    {
        public bool Success { get; set; }

        public bool LecturerExists { get; set; }

        public string? FullName { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
