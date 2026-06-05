using AllMarket.Features.Admin.Users.Dto;
using AllMarket.Infrastructure.Responses;

namespace AllMarket.Features.Admin.Users.Services;

public interface IAdminUserService
{
    Task<PaginatedResponse<AdminUserResponseDto>> GetUsersAsync(AdminUserQueryParams queryParams);
    Task<AdminUserDetailResponseDto> GetUserByIdAsync(int userId, int currentAdminUserId);
    Task<AdminUserDetailResponseDto> UpdateUserStatusAsync(int userId, AdminUpdateUserStatusDto dto, int currentAdminUserId);
}
