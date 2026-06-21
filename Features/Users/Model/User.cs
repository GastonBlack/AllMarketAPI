namespace AllMarket.Features.Users.Models;

using AllMarket.Constants.UserRoles;
using AllMarket.Features.Orders.Models;

public class User
{
    public int Id { get; set; }

    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public required string Email { get; set; }
    public required string Address { get; set; }

    public string? Phone { get; set; }
    public string Rol { get; set; } = Roles.User;

    // Verification.
    public bool EmailConfirmed { get; set; } = false;
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationExpiresAt { get; set; }
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisabledAt { get; set; }

    public List<Order> Orders { get; set; } = []; // Order history.
}
