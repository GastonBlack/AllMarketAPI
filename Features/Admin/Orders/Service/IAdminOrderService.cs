using AllMarket.Features.Admin.Orders.Dto;
using AllMarket.Infrastructure.Responses;

namespace AllMarket.Features.Admin.Orders.Services;

public interface IAdminOrderService
{
    Task<PaginatedResponse<AdminOrderResponseDto>> GetOrdersAsync(AdminOrderQueryParams queryParams);
    Task<AdminOrderDetailResponseDto> GetOrderByIdAsync(int orderId);
    Task<AdminOrderDetailResponseDto> UpdateOrderStatusAsync(int orderId, AdminUpdateOrderStatusDto dto);
}
