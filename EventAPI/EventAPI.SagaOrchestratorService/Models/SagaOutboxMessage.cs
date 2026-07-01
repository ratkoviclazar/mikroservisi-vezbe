using System.ComponentModel.DataAnnotations;

namespace EventAPI.SagaOrchestratorService.Models
{
    public class SagaOutboxMessage
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public Guid SagaId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Exchange { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string RoutingKey { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? PublishedAtUtc { get; set; }

        public bool IsPublished { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
