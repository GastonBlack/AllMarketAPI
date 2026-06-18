using AllMarket.Features.Admin.Products.Dto;
using AllMarket.Features.Products.Models;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Images;
using AllMarket.Infrastructure.Responses;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Admin.Products.Services;

public class AdminProductService : IAdminProductService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly ICacheService _cache;
    private readonly IImageStorageService _imageStorage;
    public AdminProductService(
        AllMarketDbContext db,
        ICacheService cache,
        IImageStorageService imageStorage)
    {
        _db = db;
        _cache = cache;
        _imageStorage = imageStorage;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private static AdminProductResponseDto MapToDto(Product product)
    {
        return new AdminProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            ReservedStock = product.ReservedStock,
            AvailableStock = product.Stock - product.ReservedStock,
            TotalSold = product.TotalSold,
            HasDiscount = product.HasDiscount,
            DiscountPrice = product.DiscountPrice,
            IsActive = product.IsActive,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CreatedAt = product.CreatedAt
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" => query.OrderBy(product => product.Id),
            "stock_asc" => query.OrderBy(product => product.Stock).ThenBy(product => product.Id),
            "stock_desc" => query.OrderByDescending(product => product.Stock).ThenBy(product => product.Id),
            "reserved_asc" => query.OrderBy(product => product.ReservedStock).ThenBy(product => product.Id),
            "reserved_desc" => query.OrderByDescending(product => product.ReservedStock).ThenBy(product => product.Id),
            "available_asc" => query.OrderBy(product => product.Stock - product.ReservedStock).ThenBy(product => product.Id),
            "available_desc" => query.OrderByDescending(product => product.Stock - product.ReservedStock).ThenBy(product => product.Id),
            "sold_desc" => query.OrderByDescending(product => product.TotalSold).ThenBy(product => product.Id),
            "price_asc" => query
                .OrderBy(product => product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price)
                .ThenBy(product => product.Id),
            "price_desc" => query
                .OrderByDescending(product => product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price)
                .ThenBy(product => product.Id),
            "name_asc" => query.OrderBy(product => product.Name).ThenBy(product => product.Id),
            "name_desc" => query.OrderByDescending(product => product.Name).ThenBy(product => product.Id),
            _ => throw new BadRequestException("Invalid product sortBy value.")
        };
    }

    private static void ValidateDiscount(bool hasDiscount, decimal price, decimal? discountPrice)
    {
        if (!hasDiscount && discountPrice.HasValue)
            throw new BadRequestException("Discount price must be empty when discount is disabled.");

        if (hasDiscount && (!discountPrice.HasValue || discountPrice <= 0 || discountPrice >= price))
            throw new BadRequestException("Discount price must be greater than 0 and lower than price.");
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<PaginatedResponse<AdminProductResponseDto>> GetProductsAsync(AdminProductQueryParams queryParams)
    {
        queryParams ??= new AdminProductQueryParams();

        var page = queryParams.Page < 1 ? AdminProductQueryParams.DefaultPage : queryParams.Page;
        var pageSize = queryParams.PageSize switch
        {
            < 1 => AdminProductQueryParams.DefaultPageSize,
            > AdminProductQueryParams.MaxPageSize => AdminProductQueryParams.MaxPageSize,
            _ => queryParams.PageSize
        };

        var query = _db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        query = queryParams.Status.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(product => product.IsActive),
            "disabled" => query.Where(product => !product.IsActive),
            "all" => query,
            _ => throw new BadRequestException("Invalid product status value.")
        };

        var search = queryParams.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchIsId = int.TryParse(search, out var productId);

            query = query.Where(product =>
                (searchIsId && product.Id == productId) ||
                product.Name.ToLower().Contains(search));
        }

        if (queryParams.CategoryId.HasValue)
            query = query.Where(product => product.CategoryId == queryParams.CategoryId.Value);

        if (queryParams.MinStock.HasValue)
            query = query.Where(product => product.Stock >= queryParams.MinStock.Value);

        if (queryParams.MaxStock.HasValue)
            query = query.Where(product => product.Stock <= queryParams.MaxStock.Value);

        if (queryParams.MinReservedStock.HasValue)
            query = query.Where(product => product.ReservedStock >= queryParams.MinReservedStock.Value);

        if (queryParams.MaxReservedStock.HasValue)
            query = query.Where(product => product.ReservedStock <= queryParams.MaxReservedStock.Value);

        if (queryParams.MinPrice.HasValue)
            query = query.Where(product =>
                (product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price) >= queryParams.MinPrice.Value);

        if (queryParams.MaxPrice.HasValue)
            query = query.Where(product =>
                (product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price) <= queryParams.MaxPrice.Value);

        query = ApplySorting(query, queryParams.SortBy);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => MapToDto(product))
            .ToListAsync();

        return new PaginatedResponse<AdminProductResponseDto>(items, page, pageSize, totalItems);
    }

    public async Task<AdminProductResponseDto> GetProductByIdAsync(int productId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == productId)
            ?? throw new NotFoundException("Product not found.");

        return MapToDto(product);
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<AdminProductResponseDto> CreateProductAsync(
        AdminCreateProductDto dto,
        IFormFile? image)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");
        ValidateDiscount(dto.HasDiscount, dto.Price, dto.DiscountPrice);

        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Id == dto.CategoryId);

        if (!categoryExists) throw new NotFoundException("Category not found.");

        var imageUrl = image == null
            ? null
            : await _imageStorage.UploadProductImageAsync(image);

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            Stock = dto.Stock,
            ReservedStock = 0,
            HasDiscount = dto.HasDiscount,
            DiscountPrice = dto.HasDiscount ? dto.DiscountPrice : null,
            IsActive = dto.IsActive,
            ImageUrl = imageUrl,
            CategoryId = dto.CategoryId,
            Category = null!
        };

        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsAsync();

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<AdminProductResponseDto> UpdateProductAsync(int productId, AdminUpdateProductDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");
        ValidateDiscount(dto.HasDiscount, dto.Price, dto.DiscountPrice);

        var product = await _db.Products.FindAsync(productId)
            ?? throw new NotFoundException("Product not found.");

        if (dto.Stock < dto.ReservedStock)
            throw new BadRequestException("Stock cannot be lower than reserved stock.");

        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Id == dto.CategoryId);

        if (!categoryExists) throw new NotFoundException("Category not found.");

        product.Name = dto.Name.Trim();
        product.Description = dto.Description.Trim();
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.ReservedStock = dto.ReservedStock;
        product.HasDiscount = dto.HasDiscount;
        product.DiscountPrice = dto.HasDiscount ? dto.DiscountPrice : null;
        product.IsActive = dto.IsActive;
        product.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        product.CategoryId = dto.CategoryId;

        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsAsync();

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<AdminProductResponseDto> UpdateProductStatusAsync(
        int productId,
        AdminUpdateProductStatusDto dto)
    {
        var product = await _db.Products.FindAsync(productId)
            ?? throw new NotFoundException("Product not found.");

        product.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsAsync();

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _db.Products.FindAsync(productId)
            ?? throw new NotFoundException("Product not found.");

        var productHasOrders = await _db.OrderItems
            .AsNoTracking()
            .AnyAsync(item => item.ProductId == productId);

        if (productHasOrders)
            throw new ConflictException("Product cannot be deleted because it has order history. Disable it instead.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsAsync();

        return true;
    }

    public async Task<bool> UpdateProductDiscount(int productId, AdminUpdateDiscountDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var product = await _db.Products.FindAsync(productId)
            ?? throw new NotFoundException("Product not found.");
            
        // Verifies if the request is to deactivate discount.
        if (!dto.Discount)
        {
            product.DiscountPrice = null;
            product.HasDiscount = false;

            await _db.SaveChangesAsync();
            await _cache.InvalidateProductsAsync();
            return true;
        }

        if (dto.DiscountPercentage.HasValue == dto.DiscountPrice.HasValue)
            throw new BadRequestException("Select only one option; 1. New price | 2. Percentage discount");

        // Applies discount.
        if (dto.DiscountPercentage.HasValue)
        {
            if (dto.DiscountPercentage <= 0 || dto.DiscountPercentage >= 100)
                throw new BadRequestException("Discount percentage must be greater than 0 and lower than 100.");

            product.DiscountPrice = Math.Round(
                product.Price * (1 - dto.DiscountPercentage.Value / 100),
                2,
                MidpointRounding.AwayFromZero);
        }

        if (dto.DiscountPrice.HasValue)
        {
            if (dto.DiscountPrice <= 0 || dto.DiscountPrice >= product.Price)
                throw new BadRequestException("New price must be greater than 0 and lower than the original.");

            product.DiscountPrice = dto.DiscountPrice.Value;
        }

        product.HasDiscount = true;
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsAsync();
        return true;
    }
}
