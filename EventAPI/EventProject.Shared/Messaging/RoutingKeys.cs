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
    }
}
