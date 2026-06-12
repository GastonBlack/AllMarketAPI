using AllMarket.Constants.UserRoles;
using AllMarket.Features.Admin.Users.Dto;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Responses;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Admin.Users.Services;

public class AdminUserService : IAdminUserService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public AdminUserService(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private static AdminUserResponseDto MapToListDto(User user)
    {
        return new AdminUserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Rol = user.Rol,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            DisabledAt = user.DisabledAt
        };
    }

    private static AdminUserDetailResponseDto MapToDetailDto(User user, int currentAdminUserId)
    {
        return new AdminUserDetailResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Rol = user.Rol,
            Address = user.Address,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            DisabledAt = user.DisabledAt,
            OrderCount = user.Orders.Count,
            CanBeDisabled = user.Id != currentAdminUserId && user.Rol != Roles.Admin
        };
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<PaginatedResponse<AdminUserResponseDto>> GetUsersAsync(AdminUserQueryParams queryParams)
    {
        queryParams ??= new AdminUserQueryParams();

        var page = queryParams.Page < 1 ? AdminUserQueryParams.DefaultPage : queryParams.Page;
        var pageSize = queryParams.PageSize switch
        {
            < 1 => AdminUserQueryParams.DefaultPageSize,
            > AdminUserQueryParams.MaxPageSize => AdminUserQueryParams.MaxPageSize,
            _ => queryParams.PageSize
        };

        var query = _db.Users
            .AsNoTracking()
            .AsQueryable();

        if (!queryParams.IncludeDisabled)
            query = query.Where(user => user.IsActive);

        var search = queryParams.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchIsId = int.TryParse(search, out var userId);

            query = query.Where(user =>
                (searchIsId && user.Id == userId) ||
                user.FullName.ToLower().Contains(search) ||
                user.Email.ToLower().Contains(search) ||
                (user.Phone != null && user.Phone.Contains(search)));
        }

        if (queryParams.UserId.HasValue)
            query = query.Where(user => user.Id == queryParams.UserId.Value);

        var fullName = queryParams.FullName?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(fullName))
            query = query.Where(user => user.FullName.ToLower().Contains(fullName));

        var email = queryParams.Email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(user => user.Email.ToLower().Contains(email));

        var phone = queryParams.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(user => user.Phone != null && user.Phone.Contains(phone));

        query = query.OrderBy(user => user.FullName).ThenBy(user => user.Id);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => MapToListDto(user))
            .ToListAsync();

        return new PaginatedResponse<AdminUserResponseDto>(items, page, pageSize, totalItems);
    }

    public async Task<AdminUserDetailResponseDto> GetUserByIdAsync(int userId, int currentAdminUserId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(user => user.Orders)
            .FirstOrDefaultAsync(user => user.Id == userId)
            ?? throw new NotFoundException("User not found.");

        return MapToDetailDto(user, currentAdminUserId);
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<AdminUserDetailResponseDto> UpdateUserStatusAsync(int userId, AdminUpdateUserStatusDto dto, int currentAdminUserId)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        return await SetUserStatusAsync(userId, dto.IsActive, currentAdminUserId);
    }

    public async Task<AdminUserDetailResponseDto> DisableUserAsync(int userId, int currentAdminUserId)
    {
        return await SetUserStatusAsync(userId, false, currentAdminUserId);
    }

    public async Task<AdminUserDetailResponseDto> EnableUserAsync(int userId, int currentAdminUserId)
    {
        return await SetUserStatusAsync(userId, true, currentAdminUserId);
    }

    private async Task<AdminUserDetailResponseDto> SetUserStatusAsync(int userId, bool isActive, int currentAdminUserId)
    {
        var user = await _db.Users
            .Include(user => user.Orders)
            .FirstOrDefaultAsync(user => user.Id == userId)
            ?? throw new NotFoundException("User not found.");

        if (user.Id == currentAdminUserId)
            throw new BadRequestException("Admin cannot disable their own account.");

        if (user.Rol == Roles.Admin)
            throw new BadRequestException("Admin users are not editable from this action.");

        user.IsActive = isActive;
        user.DisabledAt = isActive ? null : DateTime.UtcNow;

        if (!isActive)
        {
            await _db.RefreshTokens
                .Where(token => token.UserId == userId && token.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAt, DateTime.UtcNow));
        }

        await _db.SaveChangesAsync();

        return MapToDetailDto(user, currentAdminUserId);
    }
}
