using AllMarket.Constants.RateLimitPolicyNames;
using AllMarket.Features.Auth.Dto;
using AllMarket.Features.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AllMarket.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

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
    private CookieOptions CreateAuthCookieOptions(DateTime expiresAt, string path)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = _environment.IsDevelopment()
                    ? SameSiteMode.Lax
                    : SameSiteMode.None,
            Expires = expiresAt,
            Path = path
        };
    }

    private void SetAuthCookies(AuthSessionResult session)
    {
        Response.Cookies.Append(
            AccessTokenCookie,
            session.AccessToken,
            CreateAuthCookieOptions(session.AccessTokenExpiresAt, "/"));

        Response.Cookies.Append(
            RefreshTokenCookie,
            session.RefreshToken,
            CreateAuthCookieOptions(session.RefreshTokenExpiresAt, "/api/auth"));
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth" });
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto dto)
    {
        return Ok(await _service.RegisterAsync(dto));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        SetAuthCookies(result);

        return Ok(result.User);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> RefreshAsync()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];
        var result = await _service.RefreshAsync(refreshToken ?? string.Empty);
        SetAuthCookies(result);

        return Ok(result.User);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await _service.LogoutAsync(Request.Cookies[RefreshTokenCookie]);
        DeleteAuthCookies();

        return Ok(true);
    }
}
