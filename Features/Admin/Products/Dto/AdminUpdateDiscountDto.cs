namespace AllMarket.Features.Admin.Products.Dto;

public class AdminUpdateDiscountDto
{
    public bool Discount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountPrice { get; set; }
}
