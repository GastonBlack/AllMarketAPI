using AllMarket.Features.Categories.Models;

namespace AllMarket.Features.Products.Models;

public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; } = 0;
    public int ReservedStock { get; set; } = 0;

    public int TotalSold { get; set; } = 0;
    
    public bool HasDiscount { get; set; } = false;
    public decimal? DiscountPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }
    public required Category Category { get; set; }
}
