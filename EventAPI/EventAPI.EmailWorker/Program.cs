using EventAPI.EmailWorker.Services;
using EventAPI.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<EmailQueueConsumerHostedService>();

var host = builder.Build();
host.Run();
