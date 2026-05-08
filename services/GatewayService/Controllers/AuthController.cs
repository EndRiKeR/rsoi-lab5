using System.Text;
using System.Text.Json;
using Common.DtoModels.AuthDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers;

[ApiController]
[Route("api/v1")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [AllowAnonymous]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromBody] AuthRequest request)
    {
        const string domain = "dev-xtn38r72lorhw2oz.us.auth0.com";
        const string clientId = "5mdcjQPVkVYlch6aK7NhnGxyfQhDXVx5";
        const string clientSecret = "jm4tedMUc5nY4UhCR_X2nVkwHH8GY34EPyhI-RRUqYndgDbzTm0j7gJ13wN7szmX";
        const string audience = "https://endriker-rsoi-api";

        var tokenEndpoint = $"https://{domain}/oauth/token";

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["audience"] = audience,
            ["scope"] = "openid profile email"
        };

        using var content = new FormUrlEncodedContent(body);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(httpRequest);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        var token = JsonSerializer.Deserialize<AuthResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Ok(token);
    }
}
