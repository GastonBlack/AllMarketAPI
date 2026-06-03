using AllMarket.Features.Products.Dto;
using AllMarket.Infrastructure.Responses;

namespace AllMarket.Features.Products.Services;

public interface IProductService
{
    Task<PaginatedResponse<ProductResponseDto>> GetAllProductsAsync(ProductQueryParams queryParams);
    Task<ProductResponseDto> GetProductByIdAsync(int productId);
}
