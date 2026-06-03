namespace AllMarket.Features.Products.Dto;

public class ProductQueryParams
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyAvailable { get; set; } = false;
    public string? SortBy { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;
}
