using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.OrderItems.Dto;

public class UpdateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
