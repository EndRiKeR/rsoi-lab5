using System.Text.Json;
using Common.DtoModels.AuthDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers;

[ApiController]
[Route("api/v1")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _identityClient;
    
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger)
    {
        _identityClient = httpClientFactory.CreateClient("IdentityService");

        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize()
    {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Authorizing ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

        var redirectUri = $"http://localhost:8090/connect/authorize" +
                          $"?client_id=endriker-rsoi-api" +
                          $"&redirect_uri=http://localhost:8080/api/v1/callback" +
                          $"&response_type=code" +
                          $"&scope=openid profile email";

        return Redirect(redirectUri);
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        var tokenEndpoint = "http://identity-service:8090/connect/token";
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://gateway-service:8080/api/v1/callback",
            ["client_id"] = "endriker-rsoi-api",
            ["client_secret"] = "2YYBdhLDhhfVuen9GNq520JO3tmuqhTk"
        };
        var content = new FormUrlEncodedContent(body);
        var response = await _identityClient.PostAsync(tokenEndpoint, content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(json);

        var token = JsonSerializer.Deserialize<AuthResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // TODO: переделать мб?
        var redirectUrl = $"http://localhost:5173/callback?token={token.AccessToken}";
        return Redirect(redirectUrl);
    }
}
