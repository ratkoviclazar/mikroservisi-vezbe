namespace EventAPI.SagaOrchestratorService.DTOs
{
    public class SagaStateDto
    {
        public Guid Id { get; set; }

        public string SagaType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CurrentStep { get; set; } = string.Empty;

        public int? EventId { get; set; }

        public int? EventLectureId { get; set; }

        public int? LocationId { get; set; }

        public int? TypeId { get; set; }

        public int? LecturerId { get; set; }

        public string? ErrorMessage { get; set; }

        public string Log { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public DateTime? FailedAtUtc { get; set; }

        public DateTime? CompensatedAtUtc { get; set; }
    }
}
