using System.Security.Claims;
using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Orders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Orders.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IOrderService _service;
    public OrderController(IOrderService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutAsync([FromBody] CreateOrderDto dto)
    {
        int userId = GetAuthenticatedUserId();
        return Ok(await _service.CheckoutAsync(dto, userId));
    }

    // //////////////////////////////////////////
    // Helpers
    // //////////////////////////////////////////
    private int GetAuthenticatedUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
