using AllMarket.Features.Admin.Categories.Dto;

namespace AllMarket.Features.Admin.Categories.Services;

public interface IAdminCategoryService
{
    Task<List<AdminCategoryResponseDto>> GetCategoriesAsync(AdminCategoryQueryParams queryParams);
    Task<AdminCategoryResponseDto> CreateCategoryAsync(AdminCreateCategoryDto dto);
    Task<AdminCategoryResponseDto> UpdateCategoryAsync(AdminUpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int categoryId);
}
