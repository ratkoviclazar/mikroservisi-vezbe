namespace EventAPI.DTO.Messaging
{
    public class LecturerValidationResponse
    {
        public LecturerValidationResponse(Guid CorrelationId, bool Exists, string? FullName)
        {
            this.CorrelationId = CorrelationId;
            this.Exists = Exists;
            this.FullName = FullName;
        }

        public Guid CorrelationId { get; set; }
        public bool Exists { get; set; }
        public string? FullName { get; set; }
    }
}
