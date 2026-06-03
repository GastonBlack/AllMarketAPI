namespace AllMarket.Features.Admin.Users.Dto;

public class AdminUserDetailResponseDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Rol { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public int OrderCount { get; set; }
    public bool CanBeDisabled { get; set; }
}
