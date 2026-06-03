namespace AllMarket.Features.Payments.Dto;

public class CheckoutSessionResponseDto
{
    public required string CheckoutUrl { get; set; }
    public required string SessionId { get; set; }
}
