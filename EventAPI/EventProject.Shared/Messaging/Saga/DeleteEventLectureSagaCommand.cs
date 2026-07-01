namespace EventAPI.DTO.Messaging.Saga
{
    public class DeleteEventLectureSagaCommand : SagaMessage
    {
        public int EventLectureId { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
