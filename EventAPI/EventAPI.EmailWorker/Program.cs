using EventAPI.EmailWorker.HostedServices;
using EventAPI.EmailWorker.Messaging;
using EventAPI.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.Configure<SagaRabbitMqOptions>(
    builder.Configuration.GetSection(SagaRabbitMqOptions.SectionName));


builder.Services.AddHostedService<EmailQueueConsumerHostedService>();

builder.Services.AddHostedService<ChoreographyLocationChangeEmailConsumer>();

var host = builder.Build();
host.Run();
