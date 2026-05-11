using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Users.Dto;

public class UpdateUserEmailDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public required string Email { get; set; }
}
