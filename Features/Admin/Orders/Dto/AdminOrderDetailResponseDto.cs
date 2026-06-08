namespace AllMarket.Features.Admin.Orders.Dto;

public class AdminOrderDetailResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string UserFullName { get; set; }
    public required string UserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReservationExpiresAt { get; set; }
    public required string Status { get; set; }
    public bool CanChangeStatus { get; set; }
    public bool CanRefund { get; set; }
    public decimal TotalPrice { get; set; }
    public required List<AdminOrderItemResponseDto> Items { get; set; }
}
