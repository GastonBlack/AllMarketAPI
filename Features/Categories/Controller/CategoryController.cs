using AllMarket.Features.Categories.Dto;
using AllMarket.Features.Categories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Categories.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly ICategoryService _service;
    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetAllCategoriesAsync()
    {
        return Ok(await _service.GetAllCategoriesAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryDto dto)
    {
        return Ok(await _service.CreateCategoryAsync(dto));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch]
    public async Task<IActionResult> UpdateCategoryAsync([FromBody] UpdateCategoryDto dto)
    {
        return Ok(await _service.UpdateCategoryAsync(dto));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{categoryId}")]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] int categoryId)
    {
        return Ok(await _service.DeleteCategoryAsync(categoryId));
    }
}