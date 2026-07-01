namespace EventAPI.SagaOrchestratorService.DTOs
{
    public class CreateEventWithLecturerSagaRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Agenda { get; set; } = string.Empty;

        public DateTime DateTime { get; set; }

        public decimal DurationInHours { get; set; }

        public decimal Price { get; set; }

        public int TypeId { get; set; }

        public int LocationId { get; set; }

        public int LecturerId { get; set; }

        public DateTime LectureDateTime { get; set; }

        public decimal LectureDurationInHours { get; set; }

        public bool SimulateLectureCreationFailure { get; set; } = false;
    }
}
