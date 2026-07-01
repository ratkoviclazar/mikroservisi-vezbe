namespace EventAPI.DTO.Messaging.Saga
{
    public class EventLectureCreatedSagaReply : SagaMessage
    {
        public bool Success { get; set; }

        public int? EventLectureId { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
