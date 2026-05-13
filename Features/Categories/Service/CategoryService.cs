using AllMarket.Features.Categories.Dto;
using AllMarket.Features.Categories.Models;
using AllMarket.Helpers.Formatting;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
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
    // Class Helpers
    // //////////////////////////////////////////
    private async Task<bool> CategoryAlreadyExistsAsync(string categoryName, int? categoryIdToIgnore = null)
    {
        categoryName = NameFormatting.NormalizeString(categoryName);

        var query = _db.Categories
            .AsNoTracking()
            .Where(category => category.Name == categoryName);

        if (categoryIdToIgnore.HasValue)
        {
            query = query.Where(category => category.Id != categoryIdToIgnore.Value);
        }

        return await query.AnyAsync();
    }

    private CategoryResponseDto MapToCategoryResponseDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name
        };
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

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        dto.Name = NameFormatting.NormalizeString(dto.Name);
        if (await CategoryAlreadyExistsAsync(dto.Name)) throw new ConflictException("A category with this name already exists.");

        Category newCategory = new Category
        {
            Name = dto.Name
        };

        await _db.Categories.AddAsync(newCategory);
        await _db.SaveChangesAsync();
        return MapToCategoryResponseDto(newCategory);
    }


    public async Task<CategoryResponseDto> UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        dto.Name = NameFormatting.NormalizeString(dto.Name);
        if (await CategoryAlreadyExistsAsync(dto.Name, dto.Id)) throw new ConflictException("A category with this name already exists.");

        var categoryToUpdate = await _db.Categories.FindAsync(dto.Id) ?? throw new NotFoundException("Category not found.");
        categoryToUpdate?.Name = dto.Name;

        await _db.SaveChangesAsync();
        return MapToCategoryResponseDto(categoryToUpdate!);
    }


    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _db.Categories.FindAsync(categoryId);

        if (category == null) throw new NotFoundException("Category not found.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return true;
    }
}
