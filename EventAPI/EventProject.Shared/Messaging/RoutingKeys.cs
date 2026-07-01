namespace EventAPI.DTO.Shared
{
    public static class RoutingKeys
    {
        public const string LocationCreated = "location.created";
        public const string LocationUpdated = "location.updated";
        public const string LocationDeleted = "location.deleted";

        public const string EventTypeCreated = "eventtype.created";
        public const string EventTypeUpdated = "eventtype.updated";
        public const string EventTypeDeleted = "eventtype.deleted";

        public const string LecturerCreated = "lecturer.created";
        public const string LecturerUpdated = "lecturer.updated";
        public const string LecturerDeleted = "lecturer.deleted";

        public const string LecturerRequestQueue = "lecturer.validate.request";
        public const string LecturerReplyQueue = "events.lecturer.reply";

        public const string EmailQueue = "email.queue";
        public const string EmailSent = "email.sent";

        public const string SagaReplyQueue = "saga.reply.queue";
        public const string SagaReferenceValidateRequestQueue = "saga.reference.validate.request";
        public const string SagaLecturerValidateRequestQueue = "saga.lecturer.validate.request";
        public const string SagaEventCreateRequestQueue = "saga.event.create.request";
        public const string SagaEventDeleteRequestQueue = "saga.event.delete.request";
        public const string SagaEventLectureCreateRequestQueue = "saga.eventlecture.create.request";
        public const string SagaEventLectureDeleteRequestQueue = "saga.eventlecture.delete.request";
    }
}
