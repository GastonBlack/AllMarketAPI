namespace AllMarket.Features.Admin.Categories.Dto;

public class AdminCategoryResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int ProductCount { get; set; }
    public bool HasProducts { get; set; }
}
