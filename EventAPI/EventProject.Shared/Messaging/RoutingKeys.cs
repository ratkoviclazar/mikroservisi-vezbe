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


        public const string ChoreographyEventLocationChangeRequestQueue = "choreography.event.location.change.queue";
        public const string ChoreographyLocationReservationCancelRequestQueue = "choreography.location.reservation.cancel.queue";

        public const string LocationChangeRequested = "choreography.location.change.requested";

        public const string LocationReserved = "choreography.location.reserved";
        public const string LocationReservationFailed = "choreography.location.reservation.failed";

        public const string EventLocationChanged = "choreography.event.location.changed";
        public const string EventLocationChangeFailed = "choreography.event.location.change.failed";

        public const string LocationReservationCancelRequested = "choreography.location.reservation.cancel.requested";
        public const string LocationReservationCancelled = "choreography.location.reservation.cancelled";

        public const string LocationChangeNotificationSent = "choreography.location.change.notification.sent";
        public const string LocationChangeNotificationFailed = "choreography.location.change.notification.failed";
    }
}
