using AllMarket.Features.Auth.Dto;

namespace AllMarket.Features.Auth.Services;

public interface IAuthService
{
    public Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    public Task<(AuthResponseDto User, string Token)> LoginAsync(LoginDto dto);
}