using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Users.Dto;

public class ChangePasswordDto
{

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    public required string CurrentPassword { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    public required string NewPassword { get; set; }

    [Required(ErrorMessage = "Verification code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must contain 6 digits.")]
    public required string Code { get; set; }
}
