using Common.CircuitBreaker;
using Common.Fallbacks;
using Common.RetryQueue;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSPA", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8090";
        options.Audience = "endriker-rsoi-api";
        options.RequireHttpsMetadata = false;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = "http://identity-service:8090/",
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<CircuitBreakersController>();
builder.Services.AddSingleton<ControllersFallbacks>();

builder.Services.AddSingleton<RetryQueueService>();
builder.Services.AddHostedService<RetryBackgroundService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = "http://kafka:9092"
    };
    return new ProducerBuilder<Null, string>(config).Build();
});

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

builder.Services.AddHttpClient("StatisticService", client =>
{
    client.BaseAddress = new Uri("http://statistic-service:8100");
});

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowSPA");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
