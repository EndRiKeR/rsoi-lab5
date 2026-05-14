using BonusService.Database;
using BonusService.Database.Repositories;
using BonusService.Database.Repositories.Interfaces;
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

var connectionString = Environment.GetEnvironmentVariable("DOCKER_CONNECT_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PrivilegeContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddTransient<IPrivilegeRepository, PrivilegeRepository>();
builder.Services.AddTransient<IPrivilegeHistoryRepository, PrivilegeHistoryRepository>();

var app = builder.Build();

var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<PrivilegeContext>();
context.Database.GetPendingMigrations();
context.Database.Migrate();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run("http://0.0.0.0:8050");
