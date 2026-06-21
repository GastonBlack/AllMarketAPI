namespace AllMarket.Features.Auth.Dto;

public class VerifyEmailDto
{
    public required string Email { get; set; }
    public required string Code { get; set; }
}