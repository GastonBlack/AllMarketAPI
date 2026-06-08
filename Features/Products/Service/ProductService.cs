using AllMarket.Features.Products.Dto;
using AllMarket.Features.Products.Models;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Responses;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Products.Services;

public class ProductServices : IProductService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly ICacheService _cache;
    public ProductServices(AllMarketDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
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
    public async Task<PaginatedResponse<ProductResponseDto>> GetAllProductsAsync(ProductQueryParams queryParams)
    {
        queryParams ??= new ProductQueryParams();

        var page = queryParams.Page < 1 ? ProductQueryParams.DefaultPage : queryParams.Page;
        var pageSize = queryParams.PageSize switch
        {
            < 1 => ProductQueryParams.DefaultPageSize,
            > ProductQueryParams.MaxPageSize => ProductQueryParams.MaxPageSize,
            _ => queryParams.PageSize
        };
        var isPopular = string.Equals(
            queryParams.SortBy?.Trim(),
            "popular",
            StringComparison.OrdinalIgnoreCase);
        string? cacheKey = null;

        if (isPopular)
        {
            var version =
                await _cache.GetVersionAsync(CacheKeys.PopularProductsVersion);
            cacheKey = CacheKeys.PopularProducts(
                queryParams,
                page,
                pageSize,
                version);

            var cachedProducts =
                await _cache.GetAsync<PaginatedResponse<ProductResponseDto>>(cacheKey);

            if (cachedProducts != null) return cachedProducts;
        }

        var query = _db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .AsQueryable();

        var search = queryParams.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLower();
            query = query.Where(product =>
                product.Name.ToLower().Contains(normalizedSearch) ||
                product.Description.ToLower().Contains(normalizedSearch) ||
                product.Category.Name.ToLower().Contains(normalizedSearch));
        }

        if (queryParams.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == queryParams.CategoryId.Value);
        }

        if (queryParams.MinPrice.HasValue)
        {
            query = query.Where(product =>
                (product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price) >= queryParams.MinPrice.Value);
        }

        if (queryParams.MaxPrice.HasValue)
        {
            query = query.Where(product =>
                (product.HasDiscount && product.DiscountPrice.HasValue
                    ? product.DiscountPrice.Value
                    : product.Price) <= queryParams.MaxPrice.Value);
        }

        if (queryParams.OnlyAvailable)
        {
            query = query.Where(product => product.Stock - product.ReservedStock > 0);
        }

        query = ApplySorting(query, queryParams.SortBy);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductResponseDto
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
            })
            .ToListAsync();

        var response =
            new PaginatedResponse<ProductResponseDto>(items, page, pageSize, totalItems);

        if (cacheKey != null)
            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(1));

        return response;
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy)
    {
        var normalizedSortBy = sortBy?.Trim().ToLower();

        return normalizedSortBy switch
        {
            null or "" => query.OrderBy(product => product.Id),
            "popular" => query.OrderByDescending(product => product.TotalSold).ThenBy(product => product.Id),
            "newest" => query.OrderByDescending(product => product.CreatedAt).ThenBy(product => product.Id),
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


    public async Task<ProductResponseDto> GetProductByIdAsync(int productId)
    {
        var version =
            await _cache.GetVersionAsync(CacheKeys.ProductDetailsVersion);
        var cacheKey = CacheKeys.Product(productId, version);
        var cachedProduct = await _cache.GetAsync<ProductResponseDto>(cacheKey);

        if (cachedProduct != null) return cachedProduct;

        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product == null) throw new NotFoundException("Product not found.");

        var response = MapToProductResponseDto(product);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}
