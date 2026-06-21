using System.Security.Cryptography;
using System.Text;
using AllMarket.Features.Auth.Dto;
using AllMarket.Features.Auth.Models;
using AllMarket.Features.Auth.Security;
using AllMarket.Features.Users.Models;
using AllMarket.Helpers.Formatting;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Emails;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Auth.Services;

public class AuthService : IAuthService
{
    private const int EmailVerificationCodeExpirationMinutes = 15;
    private const int EmailVerificationResendCooldownMinutes = 3;
    private const int PasswordResetCodeExpirationMinutes = 15;
    private const int PasswordResetResendCooldownMinutes = 3;

    // Runs BCrypt even when the email does not exist to reduce timing leaks.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("dummy-password");

    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly IJwtTokenGenerator _token;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    public AuthService(
        AllMarketDbContext db,
        IJwtTokenGenerator token,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _db = db;
        _token = token;
        _configuration = configuration;
        _emailService = emailService;
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

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string EmailVerificationMessage(string verificationCode)
    {
        return $"""
            <h1>Verify your account</h1>
            <p>Use this code to verify your AllMarket account:</p>
            <p style="font-size: 28px; font-weight: bold;">{verificationCode}</p>
            <p>This code expires in 15 minutes.</p>
        """;
    }

    private static string PasswordResetMessage(string resetCode)
    {
        return $"""
            <h1>Reset your password</h1>
            <p>Use this code to reset your AllMarket password:</p>
            <p style="font-size: 28px; font-weight: bold;">{resetCode}</p>
            <p>This code expires in 15 minutes.</p>
        """;
    }

    // //////////////////////////////////////////
    // REGISTRATION
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

        // Email verification section
        var verificationCode = GenerateVerificationCode();
        var verificationCodeHash = BCrypt.Net.BCrypt.HashPassword(verificationCode);

        // Creates user.
        User newUser = new User
        {
            FullName = dto.FullName,
            PasswordHash = dto.Password,
            Email = dto.Email,
            Address = dto.Address,
            Phone = dto.Phone,

            EmailConfirmed = false,
            EmailVerificationCodeHash = verificationCodeHash,
            EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(EmailVerificationCodeExpirationMinutes),
        };

        await _db.Users.AddAsync(newUser);
        await _db.SaveChangesAsync();

        // Sends email verification email.
        await _emailService.SendAsync(
            newUser.Email,
            newUser.FullName,
            "Verify your AllMarket account",
            EmailVerificationMessage(verificationCode)
        );

        return MapToAuthResponseDto(newUser);
    }

    // //////////////////////////////////////////
    // LOG IN
    // //////////////////////////////////////////
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
        if (!user.EmailConfirmed) throw new ForbiddenException("Please verify your email before signing in.");

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

    // //////////////////////////////////////////
    // REFRESH SESSION
    // //////////////////////////////////////////
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

    // //////////////////////////////////////////
    // LOGOUT
    // //////////////////////////////////////////
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

    // //////////////////////////////////////////
    // FORGOT PASSWORD
    // //////////////////////////////////////////
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email);

        if (user == null || !user.IsActive) return true;

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

        var resetCode = GenerateVerificationCode();

        user.PasswordResetCodeHash = BCrypt.Net.BCrypt.HashPassword(resetCode);
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetCodeExpirationMinutes);

        await _db.SaveChangesAsync();

        await _emailService.SendAsync(
            user.Email,
            user.FullName,
            "Reset your AllMarket password",
            PasswordResetMessage(resetCode));

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var email = dto.Email.Trim().ToLowerInvariant();
        var code = dto.Code.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new BadRequestException("Invalid or expired password reset code.");

        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) ||
            user.PasswordResetExpiresAt == null ||
            user.PasswordResetExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException("Invalid or expired password reset code.");

        if (!BCrypt.Net.BCrypt.Verify(code, user.PasswordResetCodeHash))
            throw new BadRequestException("Invalid or expired password reset code.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetExpiresAt = null;

        await _db.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, DateTime.UtcNow));

        await _db.SaveChangesAsync();
        return true;
    }

    // //////////////////////////////////////////
    // VERIFY EMAIL
    // //////////////////////////////////////////
    public async Task<bool> VerifyEmailAsync(VerifyEmailDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var email = dto.Email.Trim().ToLowerInvariant();
        var code = dto.Code.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new NotFoundException("User not found.");

        if (user.EmailConfirmed)
            throw new ConflictException("This account is already verified. You can sign in.");

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) ||
            user.EmailVerificationExpiresAt == null ||
            user.EmailVerificationExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException("Verification code has expired.");

        var codeMatches = BCrypt.Net.BCrypt.Verify(code, user.EmailVerificationCodeHash);

        if (!codeMatches)
            throw new BadRequestException("Invalid verification code.");

        user.EmailConfirmed = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationExpiresAt = null;

        await _db.SaveChangesAsync();
        return true;
    }

    // //////////////////////////////////////////
    // RESEND EMAIL VERIFICATION CODE
    // //////////////////////////////////////////
    public async Task<bool> ResendEmailVerificationCodeAsync(ResendEmailVerificationDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new NotFoundException("User not found.");

        if (user.EmailConfirmed)
            throw new ConflictException("This account is already verified. You can sign in.");

        if (user.EmailVerificationExpiresAt.HasValue)
        {
            var resendAvailableAt = user.EmailVerificationExpiresAt.Value
                .AddMinutes(-(EmailVerificationCodeExpirationMinutes - EmailVerificationResendCooldownMinutes));

            if (DateTime.UtcNow < resendAvailableAt)
            {
                var remainingMinutes = Math.Ceiling((resendAvailableAt - DateTime.UtcNow).TotalMinutes);
                throw new ConflictException($"Please wait {remainingMinutes} minute(s) before requesting another code.");
            }
        }

        var verificationCode = GenerateVerificationCode();

        user.EmailVerificationCodeHash = BCrypt.Net.BCrypt.HashPassword(verificationCode);
        user.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(EmailVerificationCodeExpirationMinutes);

        await _db.SaveChangesAsync();

        await _emailService.SendAsync(
            user.Email,
            user.FullName,
            "Verify your AllMarket account",
            EmailVerificationMessage(verificationCode));

        return true;
    }

}
