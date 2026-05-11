using AllMarket.Features.OrderItems.Dto;

namespace AllMarket.Features.Orders.Dto;

public class OrderResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReservationExpiresAt { get; set; }
    public required List<OrderItemResponseDto> Items { get; set; }
}
