using System.ComponentModel.DataAnnotations;

namespace EventAPI.SagaOrchestratorService.Models
{
    public class SagaState
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string SagaType { get; set; } = string.Empty;

        public SagaStatus Status { get; set; }

        [Required]
        [MaxLength(150)]
        public string CurrentStep { get; set; } = string.Empty;

        public int? EventId { get; set; }

        public int? EventLectureId { get; set; }

        public int? LocationId { get; set; }

        public int? TypeId { get; set; }

        public int? LecturerId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string EventAgenda { get; set; } = string.Empty;

        public DateTime EventDateTime { get; set; }

        public decimal EventDurationInHours { get; set; }

        public decimal EventPrice { get; set; }

        public DateTime LectureDateTime { get; set; }

        public decimal LectureDurationInHours { get; set; }

        public bool SimulateLectureCreationFailure { get; set; }

        public string? ErrorMessage { get; set; }

        public string Log { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public DateTime? FailedAtUtc { get; set; }

        public DateTime? CompensatedAtUtc { get; set; }
    }
}
