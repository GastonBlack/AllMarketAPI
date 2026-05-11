using AllMarket.Features.Users.Models;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.OrderItems.Models;

namespace AllMarket.Features.Orders.Models;

public class Order
{
    public int Id { get; set; }

    // User
    public int UserId { get; set; }
    public required User User { get; set; }

    // Order items.
    public List<OrderItem> Items { get; set; } = [];

    public string Status { get; set; } = Statuses.AwaitingPayment;
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReservationExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15); // If the order expires, the ReservedStock will be released.
}
