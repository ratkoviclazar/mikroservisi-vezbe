namespace EventAPI.DTO.Messaging.Saga.Choreography
{
    public class EventLocationChanged
    {
        public Guid SagaId { get; set; }

        public Guid CorrelationId { get; set; }

        public int EventId { get; set; }

        public int OldLocationId { get; set; }

        public int NewLocationId { get; set; }

        public int ReservationId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDateTime { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
