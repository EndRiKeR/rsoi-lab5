using Common.CircuitBreaker;
using Common.Fallbacks;
using Common.RetryQueue;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// check

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8090";
        options.Audience = "endriker-rsoi-api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<CircuitBreakersController>();
builder.Services.AddSingleton<ControllersFallbacks>();

builder.Services.AddSingleton<RetryQueueService>();
builder.Services.AddHostedService<RetryBackgroundService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient("FlightService", client =>
{
    client.BaseAddress = new Uri("http://flight-service:8060");
});

builder.Services.AddHttpClient("TicketsService", client =>
{
    client.BaseAddress = new Uri("http://tickets-service:8070");
});

builder.Services.AddHttpClient("BonusService", client =>
{
    client.BaseAddress = new Uri("http://bonus-service:8050");
});

builder.Services.AddHttpClient("IdentityService", client =>
{
    client.BaseAddress = new Uri("http://identity-service:8090");
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
