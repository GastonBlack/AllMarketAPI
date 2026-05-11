using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Users.Dto;

public class UpdateUserProfileDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 250 characters.")]
    public required string Address { get; set; }

    [Phone(ErrorMessage = "Phone must be a valid phone number.")]
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }
}
