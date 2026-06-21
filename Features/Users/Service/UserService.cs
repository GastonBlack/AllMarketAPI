using AllMarket.Features.OrderItems.Dto;
using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Users.Dto;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Emails;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AllMarket.Features.Users.Services;

public class UserService : IUserService
{
    private const int PasswordResetCodeExpirationMinutes = 15;
    private const int PasswordResetResendCooldownMinutes = 3;

    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly IEmailService _emailService;
    public UserService(AllMarketDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
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

    private static string GeneratePasswordResetCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string PasswordChangeMessage(string resetCode)
    {
        return $"""
            <h1>Change your password</h1>
            <p>Use this code to change your AllMarket password:</p>
            <p style="font-size: 28px; font-weight: bold;">{resetCode}</p>
            <p>This code expires in 15 minutes.</p>
        """;
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

    public async Task<bool> RequestPasswordChangeCodeAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found.");

        if (user.PasswordResetExpiresAt.HasValue)
        {
            var resendAvailableAt = user.PasswordResetExpiresAt.Value
                .AddMinutes(-(PasswordResetCodeExpirationMinutes - PasswordResetResendCooldownMinutes));

            if (DateTime.UtcNow < resendAvailableAt)
            {
                var remainingMinutes = Math.Ceiling((resendAvailableAt - DateTime.UtcNow).TotalMinutes);
                throw new ConflictException($"Please wait {remainingMinutes} minute(s) before requesting another code.");
            }
        }

        var resetCode = GeneratePasswordResetCode();

        user.PasswordResetCodeHash = BCrypt.Net.BCrypt.HashPassword(resetCode);
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetCodeExpirationMinutes);

        await _db.SaveChangesAsync();

        await _emailService.SendAsync(
            user.Email,
            user.FullName,
            "Change your AllMarket password",
            PasswordChangeMessage(resetCode));

        return true;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto, int userId)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        if (dto.CurrentPassword == dto.NewPassword) throw new BadRequestException("New password can not be the same as the old password.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Password does not match.");

        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) ||
            user.PasswordResetExpiresAt == null ||
            user.PasswordResetExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException("Invalid or expired password reset code.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Code.Trim(), user.PasswordResetCodeHash))
            throw new BadRequestException("Invalid or expired password reset code.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetExpiresAt = null;

        await _db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, DateTime.UtcNow));
        await _db.SaveChangesAsync();

        return true;
    }
    
}
