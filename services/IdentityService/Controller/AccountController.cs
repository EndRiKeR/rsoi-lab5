using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Database.Models;

namespace IdentityService.Controller;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
            return Ok();
        }
        return BadRequest(result.Errors);
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
