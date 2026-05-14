namespace AllMarket.Features.Auth.Dto;

public class AuthResponseDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Rol { get; set; }
}
