using AllMarket.Constants.RateLimitPolicyNames;
using AllMarket.Features.Payments.Services;
using AllMarket.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AllMarket.Features.Payments.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly IPaymentService _service;
    public PaymentController(IPaymentService service)
    {
        _service = service;
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    [Authorize]
    [HttpPost("checkout/{orderId}")]
    [EnableRateLimiting(RateLimitPolicies.PaymentCheckout)]
    public async Task<IActionResult> CreateCheckoutSessionAsync([FromRoute] int orderId)
    {
        int userId = User.GetAuthenticatedUserId();
        return Ok(await _service.CreateCheckoutSessionAsync(orderId, userId));
    }

    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhookAsync()
    {
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        await _service.HandleStripeWebhookAsync(json, signatureHeader);
        return Ok();
    }
}
