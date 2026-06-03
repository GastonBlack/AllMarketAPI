using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Admin.Products.Dto;

public class AdminUpdateProductDto
{
    [Required]
    [StringLength(120)]
    public required string Name { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedStock { get; set; }

    public bool HasDiscount { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
