using System.Security.Claims;
using IdentityService.Database.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace IdentityService.Controller;

[ApiController]
[Route("connect")]
public class AuthorizationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthorizationController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [AllowAnonymous]
    public IActionResult Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest("Invalid request");

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "login.html");
        var html = System.IO.File.ReadAllText(filePath);
        return Content(html, "text/html");
    }

    [HttpPost("authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthorizePost([FromForm] string username, [FromForm] string password)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest("Invalid request");

        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return Redirect($"/connect/authorize?error=invalid_credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Redirect($"/connect/authorize?error=invalid_credentials");

        var claims = new List<Claim>
        {
            new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new Claim(OpenIddictConstants.Claims.Name, user.UserName!),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(OpenIddictConstants.Claims.PreferredUsername, user.UserName!),
            new Claim(OpenIddictConstants.Claims.Email, user.Email ?? "noemail")
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email);

        // Возвращаем SignIn – OpenIddict автоматически выполнит редирект с кодом
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest("Invalid request");

        if (request.IsAuthorizationCodeGrantType())
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = authenticateResult.Principal;
            if (principal == null)
                return Unauthorized();
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Для Password Grant – обрабатывается отдельно, если нужно
        return BadRequest("Grant type not supported here");
    }
    

    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id,
            ["preferred_username"] = user.UserName,
            ["email"] = user.Email
        };
        return Ok(claims);
    }
}