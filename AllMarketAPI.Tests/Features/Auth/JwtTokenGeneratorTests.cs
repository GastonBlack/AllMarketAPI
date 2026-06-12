using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AllMarket.Features.Auth.Security;
using AllMarket.Features.Users.Models;
using Microsoft.Extensions.Configuration;

namespace AllMarketAPI.Tests.Features.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_IncludesIdentityClaimsAndConfiguredExpiration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test_secret_key_with_at_least_32_chars",
                ["Jwt:Issuer"] = "AllMarketAPI.Tests",
                ["Jwt:Audience"] = "AllMarketClient.Tests",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();
        var generator = new JwtTokenGenerator(configuration);
        var user = new User
        {
            Id = 42,
            FullName = "Admin User",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Address = "Address",
            Rol = "Admin"
        };
        var beforeGeneration = DateTime.UtcNow;

        var result = generator.GenerateToken(user);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal("42", token.Claims.Single(claim =>
            claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, token.Claims.Single(claim =>
            claim.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.Rol, token.Claims.Single(claim =>
            claim.Type == ClaimTypes.Role).Value);
        Assert.InRange(
            result.ExpiresAt,
            beforeGeneration.AddMinutes(59),
            beforeGeneration.AddMinutes(61));
    }
}
