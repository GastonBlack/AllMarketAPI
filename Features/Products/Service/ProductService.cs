using AllMarket.Features.Products.Dto;
using AllMarket.Features.Products.Models;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Products.Services;

public class ProductServices : IProductService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public ProductServices(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private ProductResponseDto MapToProductResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            AvailableStock = product.Stock - product.ReservedStock,
            HasDiscount = product.HasDiscount,
            DiscountPrice = product.DiscountPrice,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name
        };
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<List<ProductResponseDto>> GetAllProductsAsync()
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync();
        return products.Select(MapToProductResponseDto).ToList();
    }


    public async Task<ProductResponseDto> GetProductByIdAsync(int productId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) throw new NotFoundException("Product not found.");
        return MapToProductResponseDto(product);
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        // Need to learn how to implement REDIS for cache.
        throw new NotImplementedException();
    }


    public async Task<ProductResponseDto> UpdateProductAsync(int productId, UpdateProductDto dto)
    {
        // Need to learn how to implement REDIS for cache.
        throw new NotImplementedException();
    }


    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) throw new NotFoundException("Product not found.");

        var productHasOrders = await _db.OrderItems
            .AsNoTracking()
            .AnyAsync(orderItem => orderItem.ProductId == productId);

        if (productHasOrders) throw new ConflictException("Product cannot be deleted because it has order history. Disable it instead.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return true;
    }


    public async Task<ProductResponseDto> DisableProductAsync(int productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) throw new NotFoundException("Product not found.");

        product.IsActive = false;
        await _db.SaveChangesAsync();

        return MapToProductResponseDto(product);
    }


    public async Task<ProductResponseDto> EnableProductAsync(int productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) throw new NotFoundException("Product not found.");

        product.IsActive = true;
        await _db.SaveChangesAsync();

        return MapToProductResponseDto(product);
    }


    public async Task<ProductResponseDto> ModifyStockAsync(int productId, int quantity)
    {
        if (quantity == 0) throw new BadRequestException("Quantity must be different from 0.");

        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) throw new NotFoundException("Product not found.");

        var newStock = product.Stock + quantity;
        if (newStock < product.ReservedStock) throw new BadRequestException("Stock cannot be lower than reserved stock.");

        product.Stock = newStock;
        await _db.SaveChangesAsync();

        return MapToProductResponseDto(product);
    }
}
