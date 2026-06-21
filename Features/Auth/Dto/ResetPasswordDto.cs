using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Auth.Dto;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Verification code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must contain 6 digits.")]
    public required string Code { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    public required string NewPassword { get; set; }
}
