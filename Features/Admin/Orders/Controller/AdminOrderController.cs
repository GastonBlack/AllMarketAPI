using AllMarket.Constants.UserRoles;
using AllMarket.Features.Admin.Orders.Dto;
using AllMarket.Features.Admin.Orders.Services;
using AllMarket.Features.Payments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllMarket.Features.Admin.Orders.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = Roles.Admin)]
public class AdminOrderController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IAdminOrderService _service;
    private readonly IPaymentService _paymentService;
    public AdminOrderController(IAdminOrderService service, IPaymentService paymentService)
    {
        _service = service;
        _paymentService = paymentService;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    [HttpGet]
    public async Task<IActionResult> GetOrdersAsync([FromQuery] AdminOrderQueryParams queryParams)
    {
        return Ok(await _service.GetOrdersAsync(queryParams));
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetOrderByIdAsync(int orderId)
    {
        return Ok(await _service.GetOrderByIdAsync(orderId));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [HttpPut("{orderId:int}/status")]
    public async Task<IActionResult> UpdateOrderStatusAsync(int orderId, [FromBody] AdminUpdateOrderStatusDto dto)
    {
        return Ok(await _service.UpdateOrderStatusAsync(orderId, dto));
    }

    [HttpPost("{orderId:int}/refund")]
    public async Task<IActionResult> RefundOrderAsync(int orderId)
    {
        await _paymentService.RefundOrderAsync(orderId);
        return Ok(await _service.GetOrderByIdAsync(orderId));
    }
}
