using AllMarket.Features.Auth.Dto;
using AllMarket.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IAuthService _service;
    private readonly IWebHostEnvironment _environment;
    public AuthController(IAuthService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }
    // //////////////////////////////////////////
    // Cookies
    // //////////////////////////////////////////
    private CookieOptions CreateAuthCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = _environment.IsDevelopment()
                    ? SameSiteMode.Lax
                    : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(1),
            Path = "/"
        };
    }


    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto dto)
    {
        return Ok(await _service.RegisterAsync(dto));
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto dto)
    {
        var result = await _service.LoginAsync(dto);

        Response.Cookies.Append(
            "access_token",
            result.Token,
            CreateAuthCookieOptions()
        );

        return Ok(result.User);
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token", new CookieOptions
        {
            Path = "/"
        });

        return Ok(true);
    }
}
