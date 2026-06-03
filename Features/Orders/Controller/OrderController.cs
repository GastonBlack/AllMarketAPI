using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Orders.Services;
using AllMarket.Infrastructure.Extensions;
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
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.CheckoutAsync(dto, userId));
    }
}
