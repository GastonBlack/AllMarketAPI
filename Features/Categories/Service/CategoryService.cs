using AllMarket.Features.Categories.Dto;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Categories.Services;

public class CategoryService : ICategoryService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly ICacheService _cache;
    public CategoryService(AllMarketDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync()
    {
        var cachedCategories =
            await _cache.GetAsync<List<CategoryResponseDto>>(CacheKeys.Categories);

        if (cachedCategories != null) return cachedCategories;

        var categories = await _db.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        await _cache.SetAsync(
            CacheKeys.Categories,
            categories,
            TimeSpan.FromMinutes(10));

        return categories;
    }
}
