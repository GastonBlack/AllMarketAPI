using AllMarket.Features.Categories.Dto;
using AllMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Categories.Services;

public class CategoryService : ICategoryService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public CategoryService(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
        return categories;
    }
}
