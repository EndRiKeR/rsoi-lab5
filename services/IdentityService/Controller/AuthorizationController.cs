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
    public async Task<IActionResult> Authorize()
    {
        // Для простоты вернём HTML-форму (можно заменить на SPA)
        return Content(@"
            <form method='post'>
                <input name='username' placeholder='username' />
                <input name='password' type='password' placeholder='password' />
                <button type='submit'>Login</button>
            </form>", "text/html");
    }

    [HttpPost("authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthorizePost([FromForm] string username, [FromForm] string password)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized();

        // Формируем principal для OpenIddict
        var claims = new List<Claim>
        {
            new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()),
            new Claim(OpenIddictConstants.Claims.Name, user.UserName),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(OpenIddictConstants.Claims.PreferredUsername, user.UserName),
            new Claim(OpenIddictConstants.Claims.Email, user.Email ?? "noemail")
        };

        // Добавляем роли
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email);

        // Установить куку и вернуть signin-результат
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Exchange()
    {
        _logger.LogWarning("Start token");
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request.IsAuthorizationCodeGrantType() || request.IsPasswordGrantType())
        {
            _logger.LogWarning("Good token");
            // Проверить авторизационный код и вернуть токен
            // Валидация уже выполнена OpenIddict
            var principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        _logger.LogWarning("Bad token");
        return BadRequest();
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