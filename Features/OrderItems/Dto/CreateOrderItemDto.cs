using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.OrderItems.Dto;

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Product ID is required.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
