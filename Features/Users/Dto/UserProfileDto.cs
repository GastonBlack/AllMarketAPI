namespace AllMarket.Features.Users.Dto;

public class UserProfileDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Rol { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
