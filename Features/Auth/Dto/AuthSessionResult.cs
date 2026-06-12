namespace AllMarket.Features.Auth.Dto;

public record AuthSessionResult(
    AuthResponseDto User,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
