using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StatisticService;
using StatisticService.Database;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DOCKER_CONNECT_STRING")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<StatisticContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection("Kafka:Consumer"));

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
            ValidIssuer = "http://identity-service:8090/"
        };
    });

builder.Services.AddHttpClient("GatewayService", client =>
{
    client.BaseAddress = new Uri("http://gateway-service:8080");
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StatisticContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run("http://0.0.0.0:8100");