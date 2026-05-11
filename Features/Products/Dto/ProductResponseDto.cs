namespace AllMarket.Features.Products.Dto;

public class ProductResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public bool HasDiscount { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public required string CategoryName { get; set; }
}
