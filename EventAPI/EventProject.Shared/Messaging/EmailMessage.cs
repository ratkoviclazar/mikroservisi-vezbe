namespace EventAPI.DTO.Messaging
{
    public class EmailMessage
    {
        public Guid Id { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime EnqueuedAt { get; set; }
    }
}
