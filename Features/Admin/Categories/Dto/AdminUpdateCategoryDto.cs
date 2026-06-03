using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Admin.Categories.Dto;

public class AdminUpdateCategoryDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public required string Name { get; set; }
}
