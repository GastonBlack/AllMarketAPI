namespace AllMarket.Features.Admin.Products.Dto;

public class AdminProductQueryParams
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeDisabled { get; set; } = false;
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }
    public int? MinReservedStock { get; set; }
    public int? MaxReservedStock { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;
}
