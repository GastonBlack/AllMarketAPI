namespace AllMarket.Features.Admin.Orders.Dto;

public class AdminOrderResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string UserFullName { get; set; }
    public required string UserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
    public required string Status { get; set; }
    public decimal TotalPrice { get; set; }
}
