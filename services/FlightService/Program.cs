using FlightService.Database;
using FlightService.Database.Repositories;
using FlightService.Database.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://dev-xtn38r72lorhw2oz.us.auth0.com/";
        options.Audience = "https://endriker-rsoi-api";
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://gateway-service:8080");
});

var connectionString = Environment.GetEnvironmentVariable("DOCKER_CONNECT_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

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
var pendingMigrations = context.Database.GetPendingMigrations().ToList();
context.Database.Migrate();
// if (pendingMigrations.Any())
// {
//     Console.WriteLine($"Applying {pendingMigrations.Count} migrations...");
//     context.Database.Migrate();
//     Console.WriteLine("Migrations applied successfully");
// }
// else
// {
//     Console.WriteLine("Database is up-to-date");
// }

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
