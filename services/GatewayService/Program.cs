using Common.CircuitBreaker;
using Common.Fallbacks;
using Common.RetryQueue;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://dev-xtn38r72lorhw2oz.us.auth0.com/";
        options.Audience = "https://endriker-rsoi-api";
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<CircuitBreakersController>();
builder.Services.AddSingleton<ControllersFallbacks>();

builder.Services.AddSingleton<RetryQueueService>();
builder.Services.AddHostedService<RetryBackgroundService>();

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

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
