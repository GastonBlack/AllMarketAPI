using AllMarket.Constants.UserRoles;
using AllMarket.Features.Admin.Users.Dto;
using AllMarket.Features.Admin.Users.Services;
using AllMarket.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Admin.Users.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUserController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IAdminUserService _service;
    public AdminUserController(IAdminUserService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetUsersAsync([FromQuery] AdminUserQueryParams queryParams)
    {
        return Ok(await _service.GetUsersAsync(queryParams));
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetUserByIdAsync(int userId)
    {
        return Ok(await _service.GetUserByIdAsync(userId, User.GetAuthenticatedUserId()));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPut("{userId:int}/status")]
    public async Task<IActionResult> UpdateUserStatusAsync(int userId, [FromBody] AdminUpdateUserStatusDto dto)
    {
        return Ok(await _service.UpdateUserStatusAsync(userId, dto, User.GetAuthenticatedUserId()));
    }
}
