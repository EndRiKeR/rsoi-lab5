using Common.DtoModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers;

[ApiController]
[Route("api/v1")]
public class AccountController : ControllerBase
{
    private readonly HttpClient _identityClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger)
    {
        _identityClient = httpClientFactory.CreateClient("IdentityService");
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/register")
        {
            Content = JsonContent.Create(model)
        };

        var response = await _identityClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, body);

        return Ok();
    }
}