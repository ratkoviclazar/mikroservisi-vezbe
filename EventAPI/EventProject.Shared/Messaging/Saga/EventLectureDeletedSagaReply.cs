namespace EventAPI.DTO.Messaging.Saga
{
    public class EventLectureDeletedSagaReply : SagaMessage
    {
        public bool Success { get; set; }

        public int EventLectureId { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
