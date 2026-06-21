using AllMarket.Features.Users.Dto;

namespace AllMarket.Features.Users.Services;

public interface IUserService
{
    public Task<UserProfileDto> GetUserInfoAsync(int userId);
    public Task<UserProfileDto> UpdateUserInfoAsync(UpdateUserProfileDto dto, int userId);
    public Task<UserOrderHistoryDto> GetUserOrderHistoryAsync(int userId);
    public Task<bool> RequestPasswordChangeCodeAsync(int userId);
    public Task<bool> ChangePasswordAsync(ChangePasswordDto dto, int userId);
}
