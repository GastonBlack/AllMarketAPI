using AllMarket.Constants.RateLimitPolicyNames;
using AllMarket.Features.Users.Dto;
using AllMarket.Features.Users.Services;
using AllMarket.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AllMarket.Features.Users.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // Only users with JWT Token.
public class UserController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IUserService _service;
    public UserController(IUserService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet("me")]
    public async Task<IActionResult> GetUserInfoAsync()
    {
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.GetUserInfoAsync(userId));

    }

    [HttpGet("me/history")]
    public async Task<IActionResult> GetUserOrderHistoryAsync()
    {
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.GetUserOrderHistoryAsync(userId));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPatch("update")]
    [EnableRateLimiting(RateLimitPolicies.ProfileUpdate)]
    public async Task<IActionResult> UpdateUserInfoAsync([FromBody] UpdateUserProfileDto dto)
    {
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.UpdateUserInfoAsync(dto, userId));
    }

    [HttpPut("me/password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordChange)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto dto)
    {
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.ChangePasswordAsync(dto, userId));
    }
}
