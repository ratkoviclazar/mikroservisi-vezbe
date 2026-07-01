namespace EventAPI.Messaging
{
    public class SagaRabbitMqOptions
    {
        public const string SectionName = "SagaRabbitMq";

        public string HostName { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string UserName { get; set; } = "guest";

        public string Password { get; set; } = "guest";

        public string VirtualHost { get; set; } = "/";

        public string Exchange { get; set; } = "event.exchange";
    }
}
