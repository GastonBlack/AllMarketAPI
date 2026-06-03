using AllMarket.Features.Categories.Services;
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
}
