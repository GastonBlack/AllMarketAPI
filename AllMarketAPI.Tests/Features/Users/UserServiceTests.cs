using AllMarket.Features.Auth.Models;
using AllMarket.Features.Users.Dto;
using AllMarket.Features.Users.Models;
using AllMarket.Features.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace AllMarketAPI.Tests.Features.Users;

public class UserServiceTests
{
    [Fact]
    public async Task ChangePasswordAsync_UpdatesPasswordAndRevokesSessions()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User
        {
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Current123!"),
            Address = "Test Address"
        };
        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();
        database.Db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = new string('a', 64),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            User = null!
        });
        await database.Db.SaveChangesAsync();
        var service = new UserService(database.Db);

        var changed = await service.ChangePasswordAsync(new ChangePasswordDto
        {
            CurrentPassword = "Current123!",
            NewPassword = "NewPassword123!"
        }, user.Id);

        database.Db.ChangeTracker.Clear();
        var storedUser = await database.Db.Users.SingleAsync();
        var storedToken = await database.Db.RefreshTokens.SingleAsync();

        Assert.True(changed);
        Assert.True(BCrypt.Net.BCrypt.Verify(
            "NewPassword123!",
            storedUser.PasswordHash));
        Assert.NotNull(storedToken.RevokedAt);
    }
}
