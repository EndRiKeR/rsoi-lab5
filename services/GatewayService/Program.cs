using System.Text;
using Common.CircuitBreaker;
using Common.Fallbacks;
using Common.RetryQueue;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();

var jwtSettings = configuration.GetSection("JwtSettings");
var authority = jwtSettings["Authority"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // URL OpenID конфигурации провайдера
    options.Authority = authority;
    options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
    
    // Настройки аудитории и валидации
    options.Audience = jwtSettings["Audience"];
    options.RequireHttpsMetadata = jwtSettings.GetValue<bool>("RequireHttpsMetadata");
    
    // Важные настройки для работы с JWKs
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Провайдер сам предоставляет ключи через JWKS
        ValidateIssuerSigningKey = true, // Оставляем true - ключи будут валидироваться через JWKS
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30), // Небольшой запас для рассинхронизации часов
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        
        // Дополнительные настройки
        NameClaimType = "preferred_username", // или "sub", "email" в зависимости от провайдера
        RoleClaimType = "roles" // или "role" в зависимости от провайдера
    };
    
    // Настройка событий для отладки
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenge issued: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Глобальная политика по умолчанию - требовать аутентификацию
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = defaultPolicy;
});


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
