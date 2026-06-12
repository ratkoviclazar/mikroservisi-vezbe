using EventAPI.WebPlatformService.Patterns;
using EventAPI.WebPlatformService.Services;
using EventProject.WebService.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            var msg = $"Retry attempt number: {retryCount}. Next attempt in {timespan.TotalSeconds} seconds." +
            $" Reason for error: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}";
            Console.WriteLine(msg);
        });
// Add services to the container.
builder.Services.AddControllersWithViews();

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(5));

builder.Services.AddSingleton<CircuitBreaker>(sp =>
{
    return new CircuitBreaker(2, TimeSpan.FromSeconds(30));
});

builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{

    client.BaseAddress = new Uri(builder.Configuration["Services:EventService"]!);

}).AddPolicyHandler((serviceProvider, request) =>
{
    var context = new Context();
    context["logger"] = serviceProvider.GetService<ILogger<EventApiClient>>();
    request.SetPolicyExecutionContext(context);
    return retryPolicy;
})
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient<IReferenceApiClient, ReferenceApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ReferenceService"]!);
});

builder.Services.AddHttpClient<ILecturerApiClient, LecturerApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:LecturerService"]!);
});
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
