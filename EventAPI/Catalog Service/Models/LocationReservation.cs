using EventProject.CatalogService.Models;

namespace Catalog_Service.Models
{
    public class LocationReservation
    {
        public int Id { get; set; }

        public Guid SagaId { get; set; }

        public Guid CorrelationId { get; set; }

        public int EventId { get; set; }

        public int LocationId { get; set; }

        public DateTime EventDateTime { get; set; }

        public bool IsCancelled { get; set; }

        public string? CancelReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CancelledAt { get; set; }

        public Location? Location { get; set; }
    }
}
