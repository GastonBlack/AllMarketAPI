using System.ComponentModel.DataAnnotations;
using AllMarket.Features.OrderItems.Dto;

namespace AllMarket.Features.Orders.Dto;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Order items are required.")]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public required List<CreateOrderItemDto> Items { get; set; }
}
