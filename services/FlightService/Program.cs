using FlightService.Database;
using FlightService.Database.Repositories;
using FlightService.Database.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity-service:8443";
        options.Audience = "endriker-rsoi-api";
        options.RequireHttpsMetadata = false;
    });

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Auth failed: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token valid. Claims: {string.Join(", ", context.Principal.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://gateway-service:8080");
});

var connectionString = Environment.GetEnvironmentVariable("DOCKER_CONNECT_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine(connectionString);

builder.Services.AddDbContext<FlightContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddTransient<IAirportRepository, AirportRepository>();
builder.Services.AddTransient<IFlightRepository, FlightRepository>();

builder.Services.AddScoped<DatabaseFiller>();

var app = builder.Build();
var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<FlightContext>();
context.Database.GetPendingMigrations();
context.Database.Migrate();

Console.WriteLine($"[*][*][*]Before test data");
var filler = services.GetRequiredService<DatabaseFiller>();
await filler.AddTestData();
Console.WriteLine($"[*][*][*]After test data");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run("http://0.0.0.0:8060");
