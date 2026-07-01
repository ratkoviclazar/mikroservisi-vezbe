namespace EventAPI.SagaOrchestratorService.DTOs
{
    public class CreateEventWithLecturerSagaResponse
    {
        public Guid SagaId { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? EventId { get; set; }

        public int? EventLectureId { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
