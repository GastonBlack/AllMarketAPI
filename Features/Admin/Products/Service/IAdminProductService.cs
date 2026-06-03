using AllMarket.Features.Admin.Products.Dto;
using AllMarket.Infrastructure.Responses;

namespace AllMarket.Features.Admin.Products.Services;

public interface IAdminProductService
{
    Task<PaginatedResponse<AdminProductResponseDto>> GetProductsAsync(AdminProductQueryParams queryParams);
    Task<AdminProductResponseDto> GetProductByIdAsync(int productId);
    Task<AdminProductResponseDto> CreateProductAsync(AdminCreateProductDto dto);
    Task<AdminProductResponseDto> UpdateProductAsync(int productId, AdminUpdateProductDto dto);
    Task<bool> DeleteProductAsync(int productId);
}
