namespace EventAPI.SagaOrchestratorService.Models
{
    public enum SagaStatus
    {
        Started = 1,

        ReferenceDataValidated = 2,

        LecturerValidated = 3,

        EventCreated = 4,

        EventLectureCreated = 5,

        Completed = 6,

        Compensating = 7,

        Compensated = 8,

        Failed = 9
    }
}
