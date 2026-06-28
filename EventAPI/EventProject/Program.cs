using EventAPI.Data;
using EventAPI.HostedServices;
using EventAPI.Messaging;
using EventAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServer<EventsDbContext>(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.Configure<RabbitMqConsumerOptions>(
    builder.Configuration.GetSection(RabbitMqConsumerOptions.SectionName));

builder.Services.AddScoped<MessageDispatcher>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService>();


builder.Services.AddSingleton<IEmailPublisher, RabbitMqEmailPublisher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();

builder.Services.AddSingleton<RequestReplyClient>();
builder.Services.AddSingleton<IRequestReplyClient>(sp => sp.GetRequiredService<RequestReplyClient>());

builder.Services.AddHostedService<ReplyConsumerHostedService>();
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
