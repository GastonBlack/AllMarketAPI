using AllMarket.Features.Categories.Dto;

namespace AllMarket.Features.Categories.Services;

public interface ICategoryService
{
    public Task<List<CategoryResponseDto>> GetAllCategoriesAsync();
}
