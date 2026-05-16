using IdentityService.Database;
using IdentityService.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DOCKER_CONNECT_STRING")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
                .SetTokenEndpointUris("/connect/token")
                .SetUserInfoEndpointUris("/connect/userinfo")

                .SetIssuer(new Uri("http://identity-service:8090"))

                .AllowAuthorizationCodeFlow()
                .AllowPasswordFlow()

                .RegisterScopes(OpenIddictConstants.Scopes.OpenId,
                               OpenIddictConstants.Scopes.Profile,
                               OpenIddictConstants.Scopes.Email)
               
                .AddDevelopmentSigningCertificate()
                .AddEphemeralEncryptionKey()
                .DisableAccessTokenEncryption()
                .UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri("http://gateway-service:8080");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        Console.WriteLine($"Applying {pendingMigrations.Count()} pending migrations...");
        await db.Database.MigrateAsync();
    }
    else
    {
        Console.WriteLine("No pending migrations.");
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var clientId = "endriker-rsoi-api";
    var existingClient = await manager.FindByClientIdAsync(clientId);

    var descriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = clientId,
        ClientSecret = "2YYBdhLDhhfVuen9GNq520JO3tmuqhTk",
        ClientType = OpenIddictConstants.ClientTypes.Confidential,
        DisplayName = "RSOI API",
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Email
        },
        RedirectUris = { new Uri("http://localhost:8080/api/v1/callback") },
        PostLogoutRedirectUris = { new Uri("http://localhost:8080") }
    };

    if (existingClient is null)
        await manager.CreateAsync(descriptor);
    else
        await manager.UpdateAsync(existingClient, descriptor);
    
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync("rsoi_user") == null)
    {
        var user = new ApplicationUser { UserName = "rsoi_user", Email = "rsoi_user@example.com" };
        var result = await userManager.CreateAsync(user, "6Pm-GuZ-LeN-Zqy");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    }
    
    var admin = await userManager.FindByNameAsync("admin");
    if (admin == null)
    {
        admin = new ApplicationUser { UserName = "admin", Email = "admin@example.com" };
        await userManager.CreateAsync(admin, "6Pm-GuZ-LeN-Zqy");
    }
    if (!await userManager.IsInRoleAsync(admin, "Admin"))
    {
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run("http://0.0.0.0:8090");