using AllMarket.Features.Orders.Models;
using AllMarket.Features.Products.Models;

namespace AllMarket.Features.OrderItems.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public required Order Order { get; set; }

    public int ProductId { get; set; }
    public required Product Product { get; set; }

    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}