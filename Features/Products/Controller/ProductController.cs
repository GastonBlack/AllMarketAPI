using AllMarket.Features.Products.Dto;
using AllMarket.Features.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Products.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IProductService _service;
    public ProductController(IProductService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetAllProductsAsync([FromQuery] ProductQueryParams queryParams)
    {
        return Ok(await _service.GetAllProductsAsync(queryParams));
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetProductByIdAsync([FromRoute] int productId)
    {
        return Ok(await _service.GetProductByIdAsync(productId));
    }

}
