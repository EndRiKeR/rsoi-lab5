using Common.DtoModels;
using IdentityService.Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            _logger.LogInformation($"User '{model.Username}' registered successfully.");
            return Ok();
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        _logger.LogInformation($"Registration failed for '{model.Username}': {string.Join(", ", errors)}");
        return BadRequest(new { errors });
    }

    [HttpPost("create-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] RegisterModel model)
    {
        var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            return Ok();
        }
        return BadRequest(result.Errors);
    }
}
