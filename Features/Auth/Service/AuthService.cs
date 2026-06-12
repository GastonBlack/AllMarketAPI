using System.Security.Cryptography;
using System.Text;
using AllMarket.Features.Auth.Dto;
using AllMarket.Features.Auth.Models;
using AllMarket.Features.Auth.Security;
using AllMarket.Features.Users.Models;
using AllMarket.Helpers.Formatting;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Auth.Services;

public class AuthService : IAuthService
{
    // Runs BCrypt even when the email does not exist to reduce timing leaks.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("dummy-password");

    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly IJwtTokenGenerator _token;
    private readonly IConfiguration _configuration;
    public AuthService(
        AllMarketDbContext db,
        IJwtTokenGenerator token,
        IConfiguration configuration)
    {
        _db = db;
        _token = token;
        _configuration = configuration;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private AuthResponseDto MapToAuthResponseDto(User user)
    {
        return new AuthResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Rol = user.Rol
        };
    }

    private int GetRefreshTokenExpirationDays()
    {
        if (!int.TryParse(
                _configuration["Jwt:RefreshTokenExpirationDays"],
                out var expirationDays) ||
            expirationDays <= 0)
        {
            throw new InvalidOperationException(
                "JWT refresh token expiration days is not configured.");
        }

        return expirationDays;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    private AuthSessionResult CreateSessionResult(
        User user,
        string refreshToken,
        DateTime refreshTokenExpiresAt)
    {
        var accessToken = _token.GenerateToken(user);

        return new AuthSessionResult(
            MapToAuthResponseDto(user),
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private async Task RevokeTokenFamilyAsync(Guid familyId)
    {
        var revokedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, revokedAt));
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        // Validates if there is another user with the same email.
        dto.Email = dto.Email.Trim().ToLowerInvariant();
        bool emailExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == dto.Email);

        if (emailExists) throw new ConflictException("Email is already registered.");

        // Normalizes data.
        dto.FullName = NameFormatting.NormalizeString(dto.FullName);
        dto.Address = NameFormatting.NormalizeString(dto.Address);
        if (!dto.Phone.IsWhiteSpace()) dto.Phone = NumberFormatting.RemoveNumberSpaces(dto.Phone!);

        // Hashes Password
        dto.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Creates user.
        User newUser = new User
        {
            FullName = dto.FullName,
            PasswordHash = dto.Password,
            Email = dto.Email,
            Address = dto.Address,
            Phone = dto.Phone,
        };

        await _db.Users.AddAsync(newUser);
        await _db.SaveChangesAsync();

        return MapToAuthResponseDto(newUser);
    }

    public async Task<AuthSessionResult> LoginAsync(LoginDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        dto.Email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        var passwordHash = user?.PasswordHash ?? DummyPasswordHash;
        bool passwordMatch = BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash);

        if (user == null || !passwordMatch) throw new BadRequestException("Email/password incorrect.");
        if (!user.IsActive) throw new ForbiddenException("User account is disabled.");

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());

        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            TokenHash = HashRefreshToken(refreshToken),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = refreshTokenExpiresAt,
            UserId = user.Id,
            User = null!
        });
        await _db.SaveChangesAsync();

        return CreateSessionResult(user, refreshToken, refreshTokenExpiresAt);
    }

    public async Task<AuthSessionResult> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException("Refresh token is missing.");

        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _db.RefreshTokens
            .AsNoTracking()
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash)
            ?? throw new UnauthorizedException("Refresh token is invalid.");

        if (storedToken.RevokedAt.HasValue)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId);
            throw new UnauthorizedException("Refresh token is no longer valid.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow || !storedToken.User.IsActive)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId);
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var newRefreshToken = GenerateRefreshToken();
        var newTokenHash = HashRefreshToken(newRefreshToken);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
        var revokedAt = DateTime.UtcNow;
        var rotationSucceeded = false;

        await using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            var updatedRows = await _db.RefreshTokens
                .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.ReplacedByTokenHash, newTokenHash));

            if (updatedRows == 1)
            {
                await _db.RefreshTokens.AddAsync(new RefreshToken
                {
                    TokenHash = newTokenHash,
                    FamilyId = storedToken.FamilyId,
                    ExpiresAt = newRefreshTokenExpiresAt,
                    UserId = storedToken.UserId,
                    User = null!
                });
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                rotationSucceeded = true;
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }

        if (!rotationSucceeded)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId);
            throw new UnauthorizedException("Refresh token is no longer valid.");
        }

        return CreateSessionResult(
            storedToken.User,
            newRefreshToken,
            newRefreshTokenExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = HashRefreshToken(refreshToken);
        var familyId = await _db.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync();

        if (familyId.HasValue)
            await RevokeTokenFamilyAsync(familyId.Value);
    }
}
