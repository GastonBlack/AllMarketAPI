using AllMarket.Features.OrderItems.Dto;
using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Users.Dto;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Users.Services;

public class UserService : IUserService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public UserService(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Helpers
    // //////////////////////////////////////////
    private UserProfileDto MapToUserProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Rol = user.Rol,
            Address = user.Address,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt
        };
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<UserProfileDto> GetUserInfoAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        return MapToUserProfileDto(user);
    }

    public async Task<UserProfileDto> UpdateUserInfoAsync(UpdateUserProfileDto dto, int userId)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var user = await _db.Users.FindAsync(userId);

        if (user == null) throw new NotFoundException("User not found.");

        if (user.FullName == dto.FullName &&
            user.Address == dto.Address &&
            user.Phone == dto.Phone
        ) throw new BadRequestException("User's information is the same.");

        user.FullName = dto.FullName;
        user.Address = dto.Address;
        user.Phone = dto.Phone;

        await _db.SaveChangesAsync();
        return MapToUserProfileDto(user);
    }

    public async Task<UserOrderHistoryDto> GetUserOrderHistoryAsync(int userId)
    {
        var history = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserOrderHistoryDto
            {
                UserId = user.Id,
                Orders = user.Orders
                    .OrderByDescending(order => order.CreatedAt)
                    .Select(order => new OrderResponseDto
                    {
                        Id = order.Id,
                        UserId = order.UserId,
                        Status = order.Status,
                        TotalPrice = order.TotalPrice,
                        CreatedAt = order.CreatedAt,
                        ReservationExpiresAt = order.ReservationExpiresAt,
                        Items = order.Items
                            .Select(item => new OrderItemResponseDto
                            {
                                Id = item.Id,
                                OrderId = item.OrderId,
                                ProductId = item.ProductId,
                                ProductName = item.Product.Name,
                                Quantity = item.Quantity,
                                PriceAtPurchase = item.PriceAtPurchase,
                                Subtotal = item.Quantity * item.PriceAtPurchase
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return history ?? throw new NotFoundException("User not found.");
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto, int userId)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        if (dto.CurrentPassword == dto.NewPassword) throw new BadRequestException("New password can not be the same as the old password.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Password does not match.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return true;
    }
    
}
