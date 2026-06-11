using AllMarket.Constants.RateLimitPolicyNames;
using AllMarket.Constants.UserRoles;
using AllMarket.Features.Admin.Products.Dto;
using AllMarket.Features.Admin.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AllMarket.Features.Admin.Products.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = Roles.Admin)]
public class AdminProductController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IAdminProductService _service;
    public AdminProductController(IAdminProductService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetProductsAsync([FromQuery] AdminProductQueryParams queryParams)
    {
        return Ok(await _service.GetProductsAsync(queryParams));
    }

    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetProductByIdAsync(int productId)
    {
        return Ok(await _service.GetProductByIdAsync(productId));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(RateLimitPolicies.ProductCreation)]
    public async Task<IActionResult> CreateProductAsync(
        [FromForm] AdminCreateProductDto dto,
        IFormFile? image)
    {
        return Ok(await _service.CreateProductAsync(dto, image));
    }

    [HttpPut("{productId:int}")]
    public async Task<IActionResult> UpdateProductAsync(int productId, [FromBody] AdminUpdateProductDto dto)
    {
        return Ok(await _service.UpdateProductAsync(productId, dto));
    }

    [HttpPut("{productId:int}/status")]
    public async Task<IActionResult> UpdateProductStatusAsync(
        int productId,
        [FromBody] AdminUpdateProductStatusDto dto)
    {
        return Ok(await _service.UpdateProductStatusAsync(productId, dto));
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> DeleteProductAsync(int productId)
    {
        return Ok(await _service.DeleteProductAsync(productId));
    }
}
