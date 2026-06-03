using AllMarket.Features.Admin.Categories.Dto;
using AllMarket.Features.Categories.Models;
using AllMarket.Helpers.Formatting;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Admin.Categories.Services;

public class AdminCategoryService : IAdminCategoryService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public AdminCategoryService(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private static AdminCategoryResponseDto MapToDto(Category category)
    {
        return new AdminCategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    private async Task<bool> CategoryAlreadyExistsAsync(string categoryName, int? categoryIdToIgnore = null)
    {
        var normalizedName = NameFormatting.NormalizeString(categoryName);

        var query = _db.Categories
            .AsNoTracking()
            .Where(category => category.Name == normalizedName);

        if (categoryIdToIgnore.HasValue)
            query = query.Where(category => category.Id != categoryIdToIgnore.Value);

        return await query.AnyAsync();
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<List<AdminCategoryResponseDto>> GetCategoriesAsync(AdminCategoryQueryParams queryParams)
    {
        queryParams ??= new AdminCategoryQueryParams();

        var query = _db.Categories
            .AsNoTracking()
            .AsQueryable();

        var search = queryParams.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(category => category.Name.ToLower().Contains(search));

        return await query
            .OrderBy(category => category.Name)
            .Select(category => MapToDto(category))
            .ToListAsync();
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<AdminCategoryResponseDto> CreateCategoryAsync(AdminCreateCategoryDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var normalizedName = NameFormatting.NormalizeString(dto.Name);
        if (await CategoryAlreadyExistsAsync(normalizedName))
            throw new ConflictException("A category with this name already exists.");

        var category = new Category
        {
            Name = normalizedName
        };

        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<AdminCategoryResponseDto> UpdateCategoryAsync(AdminUpdateCategoryDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var normalizedName = NameFormatting.NormalizeString(dto.Name);
        if (await CategoryAlreadyExistsAsync(normalizedName, dto.Id))
            throw new ConflictException("A category with this name already exists.");

        var category = await _db.Categories.FindAsync(dto.Id)
            ?? throw new NotFoundException("Category not found.");

        category.Name = normalizedName;
        await _db.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _db.Categories.FindAsync(categoryId)
            ?? throw new NotFoundException("Category not found.");

        var categoryHasProducts = await _db.Products
            .AsNoTracking()
            .AnyAsync(product => product.CategoryId == categoryId);

        if (categoryHasProducts)
            throw new ConflictException("Category cannot be deleted because it has products.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return true;
    }
}
