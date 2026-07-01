namespace EventAPI.DTO.Messaging.Saga
{
    public class ValidateLecturerCommand : SagaMessage
    {
        public int LecturerId { get; set; }

        public string ReplyTo { get; set; } = string.Empty;
    }
}
