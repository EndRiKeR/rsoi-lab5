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
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizePassword([FromBody] AuthRequest request)
    {
        _logger.LogInformation("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Authorizing password ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        
        var tokenEndpoint = "http://identity-service:8090/connect/token";
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["client_id"] = "endriker-rsoi-api",
            ["client_secret"] = "2YYBdhLDhhfVuen9GNq520JO3tmuqhTk", // заменить на конфигурацию
            ["scope"] = "openid profile email"
        };

        using var content = new FormUrlEncodedContent(body);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = content
        };

        var response = await _identityClient.SendAsync(httpRequest);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        var token = JsonSerializer.Deserialize<AuthResponse>(json);
        return Ok(token);
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

        var token = JsonSerializer.Deserialize<AuthResponse>(json);
        return Ok(token);
    }
}
