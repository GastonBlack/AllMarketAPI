using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Admin.Orders.Dto;

public class AdminUpdateOrderStatusDto
{
    [Required]
    [StringLength(40)]
    public required string Status { get; set; }
}
