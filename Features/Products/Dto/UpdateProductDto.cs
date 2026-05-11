using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Products.Dto;

public class UpdateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 120 characters.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Product description is required.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Product description must be between 10 and 1000 characters.")]
    public required string Description { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Reserved stock cannot be negative.")]
    public int ReservedStock { get; set; }

    public bool HasDiscount { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "Discount price must be greater than 0.")]
    public decimal? DiscountPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [Url(ErrorMessage = "Image URL must be a valid URL.")]
    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }
}
