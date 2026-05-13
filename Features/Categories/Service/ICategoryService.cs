using AllMarket.Features.Categories.Dto;

namespace AllMarket.Features.Categories.Services;

public interface ICategoryService
{
    public Task<List<CategoryResponseDto>> GetAllCategoriesAsync();
    public Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto);
    public Task<CategoryResponseDto> UpdateCategoryAsync(UpdateCategoryDto dto);
    public Task<bool> DeleteCategoryAsync(int categoryId);
}