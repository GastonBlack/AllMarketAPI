using System.Security.Claims;
using AllMarket.Features.Users.Dto;
using AllMarket.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        int userId = GetAuthenticatedUserId();
        return Ok(await _service.GetUserInfoAsync(userId));

    }

    [HttpGet("me/history")]
    public async Task<IActionResult> GetUserOrderHistoryAsync()
    {
        int userId = GetAuthenticatedUserId();
        return Ok(await _service.GetUserOrderHistoryAsync(userId));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPatch("update")]
    public async Task<IActionResult> UpdateUserInfoAsync([FromBody] UpdateUserProfileDto dto)
    {
        int userId = GetAuthenticatedUserId();
        return Ok(await _service.UpdateUserInfoAsync(dto, userId));
    }

    // //////////////////////////////////////////
    // Helpers
    // //////////////////////////////////////////
    private int GetAuthenticatedUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}