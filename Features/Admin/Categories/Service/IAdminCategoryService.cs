using AllMarket.Features.Admin.Categories.Dto;
using AllMarket.Infrastructure.Responses;

namespace AllMarket.Features.Admin.Categories.Services;

public interface IAdminCategoryService
{
    Task<PaginatedResponse<AdminCategoryResponseDto>> GetCategoriesAsync(AdminCategoryQueryParams queryParams);
    Task<AdminCategoryResponseDto> CreateCategoryAsync(AdminCreateCategoryDto dto);
    Task<AdminCategoryResponseDto> UpdateCategoryAsync(AdminUpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int categoryId);
}
