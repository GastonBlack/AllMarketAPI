namespace AllMarket.Features.Auth.Security;

public record JwtTokenResult(string Token, DateTime ExpiresAt);
