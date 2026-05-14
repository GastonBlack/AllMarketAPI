using AllMarket.Features.Products.Dto;

namespace AllMarket.Features.Products.Services;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllProductsAsync();
    Task<ProductResponseDto> GetProductByIdAsync(int productId);

    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductResponseDto> UpdateProductAsync(int productId, UpdateProductDto dto);
    Task<ProductResponseDto> DisableProductAsync(int productId);
    Task<ProductResponseDto> EnableProductAsync(int productId);
    Task<bool> DeleteProductAsync(int productId);
    
    Task<ProductResponseDto> ModifyStockAsync(int productId, int quantity);

}
