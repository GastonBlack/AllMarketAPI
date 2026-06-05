namespace AllMarket.Features.Admin.Categories.Dto;

public class AdminCategoryQueryParams
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public string? Search { get; set; }
    public bool OnlyWithoutProducts { get; set; } = false;
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;
}
