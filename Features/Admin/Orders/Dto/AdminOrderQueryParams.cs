namespace AllMarket.Features.Admin.Orders.Dto;

public class AdminOrderQueryParams
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public string? Search { get; set; }
    public int? OrderId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;
}
