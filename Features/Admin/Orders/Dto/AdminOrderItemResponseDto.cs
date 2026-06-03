namespace AllMarket.Features.Admin.Orders.Dto;

public class AdminOrderItemResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public decimal Subtotal { get; set; }
}
