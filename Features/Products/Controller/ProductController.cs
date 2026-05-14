using AllMarket.Features.Products.Dto;
using AllMarket.Features.Products.Services;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<IActionResult> GetAllProductsAsync()
    {
        return Ok(await _service.GetAllProductsAsync());
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetProductByIdAsync([FromRoute] int productId)
    {
        return Ok(await _service.GetProductByIdAsync(productId));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductDto dto)
    {
        return Ok(await _service.CreateProductAsync(dto));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{productId}")]
    public async Task<IActionResult> UpdateProductAsync([FromRoute] int productId, [FromBody] UpdateProductDto dto)
    {
        return Ok(await _service.UpdateProductAsync(productId, dto));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{productId}/disable")]
    public async Task<IActionResult> DisableProductAsync([FromRoute] int productId)
    {
        return Ok(await _service.DisableProductAsync(productId));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{productId}/enable")]
    public async Task<IActionResult> EnableProductAsync([FromRoute] int productId)
    {
        return Ok(await _service.EnableProductAsync(productId));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteProductAsync([FromRoute] int productId)
    {
        return Ok(await _service.DeleteProductAsync(productId));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{productId}/stock")]
    public async Task<IActionResult> ModifyStockAsync([FromRoute] int productId, [FromQuery] int quantity)
    {
        return Ok(await _service.ModifyStockAsync(productId, quantity));
    }
}
