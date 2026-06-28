namespace EventAPI.DTO.Messaging
{
    public class LecturerValidationRequest
    {

        public LecturerValidationRequest(Guid correlationId, int lecturerId, string replyTo)
        {
            CorrelationId = correlationId;
            LecturerId = lecturerId;
            ReplyTo = replyTo;
        }

        public Guid CorrelationId { get; set; }
        public int LecturerId { get; set; }
        public string ReplyTo { get; set; } = "";
    }
}
