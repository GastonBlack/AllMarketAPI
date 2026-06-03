using AllMarket.Features.Orders.Dto;

namespace AllMarket.Features.Orders.Services;

public interface IOrderService
{
    Task<OrderResponseDto> CheckoutAsync(CreateOrderDto dto, int userId);
}
