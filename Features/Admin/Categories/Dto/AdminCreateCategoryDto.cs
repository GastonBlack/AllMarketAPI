using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Admin.Categories.Dto;

public class AdminCreateCategoryDto
{
    [Required]
    [StringLength(60)]
    public required string Name { get; set; }
}
