using EventAPI.SagaOrchestratorService.Choreography;
using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.HostedServices;
using EventAPI.SagaOrchestratorService.Messaging;
using EventAPI.SagaOrchestratorService.Options;
using EventAPI.SagaOrchestratorService.Orchestration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.Configure<ServiceExchangesOptions>(
    builder.Configuration.GetSection("ServiceExchanges"));

builder.Services.AddScoped<ISagaMessagePublisher, SagaRabbitMqPublisher>();
builder.Services.AddScoped<ISagaReplyHandler, SagaReplyHandler>();
builder.Services.AddScoped<ISagaOutboxService, SagaOutboxService>();

builder.Services.AddScoped<ICreateEventWithLecturerSagaOrchestrator, CreateEventWithLecturerSagaOrchestrator>();

builder.Services.AddHostedService<SagaReplyConsumerHostedService>();
builder.Services.AddHostedService<SagaOutboxDispatcherHostedService>();

builder.Services.AddHostedService<LocationChangeSagaTrackerConsumer>();

builder.Services.AddDbContext<SagaDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();