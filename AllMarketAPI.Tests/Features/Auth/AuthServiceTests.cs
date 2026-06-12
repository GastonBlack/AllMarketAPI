using System.Security.Cryptography;
using System.Text;
using AllMarket.Features.Auth.Dto;
using AllMarket.Features.Auth.Security;
using AllMarket.Features.Auth.Services;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AllMarketAPI.Tests.Features.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_StoresOnlyRefreshTokenHash()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = CreateUser();
        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();
        var service = CreateService(database.Db);

        var session = await service.LoginAsync(new LoginDto
        {
            Email = user.Email,
            Password = "Password123!"
        });

        var storedToken = await database.Db.RefreshTokens.SingleAsync();
        var expectedHash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(session.RefreshToken)));

        Assert.Equal(expectedHash, storedToken.TokenHash);
        Assert.NotEqual(session.RefreshToken, storedToken.TokenHash);
        Assert.Equal(user.Id, session.User.Id);
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndRejectsReuse()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = CreateUser();
        database.Db.Users.Add(user);
        await database.Db.SaveChangesAsync();
        var service = CreateService(database.Db);
        var login = await service.LoginAsync(new LoginDto
        {
            Email = user.Email,
            Password = "Password123!"
        });

        var refreshed = await service.RefreshAsync(login.RefreshToken);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.RefreshAsync(login.RefreshToken));

        database.Db.ChangeTracker.Clear();
        var tokens = await database.Db.RefreshTokens
            .OrderBy(token => token.Id)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, token => Assert.NotNull(token.RevokedAt));
        Assert.Equal(tokens[1].TokenHash, tokens[0].ReplacedByTokenHash);
    }

    private static AuthService CreateService(AllMarket.Infrastructure.Data.AllMarketDbContext db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        return new AuthService(db, new TestJwtTokenGenerator(), configuration);
    }

    private static User CreateUser()
    {
        return new User
        {
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Address = "Test Address"
        };
    }

    private sealed class TestJwtTokenGenerator : IJwtTokenGenerator
    {
        public JwtTokenResult GenerateToken(User user)
        {
            return new JwtTokenResult("access-token", DateTime.UtcNow.AddHours(1));
        }
    }
}
