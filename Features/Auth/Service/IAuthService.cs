using AllMarket.Features.Auth.Dto;

namespace AllMarket.Features.Auth.Services;

public interface IAuthService
{
    public Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    public Task<AuthSessionResult> LoginAsync(LoginDto dto);
    public Task<AuthSessionResult> RefreshAsync(string refreshToken);
    public Task LogoutAsync(string? refreshToken);
}
