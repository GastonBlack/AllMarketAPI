using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Orders.Dto;

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Order status is required.")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Order status must be between 3 and 40 characters.")]
    public required string Status { get; set; }
}
