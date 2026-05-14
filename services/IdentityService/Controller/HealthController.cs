using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controller;

[ApiController]
[Route("manage")]
public class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        return Ok("Healthy");
    }
}