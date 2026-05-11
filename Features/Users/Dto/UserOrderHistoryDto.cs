using AllMarket.Features.Orders.Dto;

namespace AllMarket.Features.Users.Dto;

public class UserOrderHistoryDto
{
    public int UserId { get; set; }
    public required List<OrderResponseDto> Orders { get; set; }
}
