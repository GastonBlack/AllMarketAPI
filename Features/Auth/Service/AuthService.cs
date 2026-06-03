using AllMarket.Features.Auth.Dto;
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
    public AuthService(AllMarketDbContext db, IJwtTokenGenerator token)
    {
        _db = db;
        _token = token;
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

    public async Task<(AuthResponseDto User, string Token)> LoginAsync(LoginDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        dto.Email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        var passwordHash = user?.PasswordHash ?? DummyPasswordHash;
        bool passwordMatch = BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash);

        if (user == null || !passwordMatch) throw new BadRequestException("Email/password incorrect.");

        // Generates JWT token.
        var token = _token.GenerateToken(user);

        return (MapToAuthResponseDto(user), token);
    }
}
