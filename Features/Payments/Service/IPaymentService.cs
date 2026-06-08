using AllMarket.Features.Payments.Dto;

namespace AllMarket.Features.Payments.Services;

public interface IPaymentService
{
    Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(int orderId, int userId);
    Task HandleStripeWebhookAsync(string json, string signatureHeader);
    Task RefundOrderAsync(int orderId);
}
