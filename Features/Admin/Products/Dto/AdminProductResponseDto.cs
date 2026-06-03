namespace AllMarket.Features.Admin.Products.Dto;

public class AdminProductResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int ReservedStock { get; set; }
    public int AvailableStock { get; set; }
    public int TotalSold { get; set; }
    public bool HasDiscount { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
}
