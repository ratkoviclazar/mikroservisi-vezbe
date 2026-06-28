namespace EventAPI.Services
{
    public class RabbitMqConsumerOptions
    {
        public const string SectionName = "RabbitMq";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";

        public List<RabbitMqBinding> Bindings { get; set; } = new();
    }

    public class RabbitMqBinding
    {
        public string Exchange { get; set; } = "";
        public string Queue { get; set; } = "";
        public string[] RoutingKeys { get; set; } = Array.Empty<string>();

    }
}
