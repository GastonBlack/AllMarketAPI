using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Auth.Dto;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public required string Email { get; set; }
}
