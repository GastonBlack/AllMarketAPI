using AllMarket.Features.Users.Models;

namespace AllMarket.Features.Auth.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public required string TokenHash { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public int UserId { get; set; }
    public required User User { get; set; }
}
