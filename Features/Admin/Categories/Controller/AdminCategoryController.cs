using AllMarket.Constants.UserRoles;
using AllMarket.Features.Admin.Categories.Dto;
using AllMarket.Features.Admin.Categories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Admin.Categories.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = Roles.Admin)]
public class AdminCategoryController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IAdminCategoryService _service;
    public AdminCategoryController(IAdminCategoryService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetCategoriesAsync([FromQuery] AdminCategoryQueryParams queryParams)
    {
        return Ok(await _service.GetCategoriesAsync(queryParams));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] AdminCreateCategoryDto dto)
    {
        return Ok(await _service.CreateCategoryAsync(dto));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCategoryAsync([FromBody] AdminUpdateCategoryDto dto)
    {
        return Ok(await _service.UpdateCategoryAsync(dto));
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> DeleteCategoryAsync(int categoryId)
    {
        return Ok(await _service.DeleteCategoryAsync(categoryId));
    }
}
