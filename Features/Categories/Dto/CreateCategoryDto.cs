using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Categories.Dto;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 60 characters.")]
    public required string Name { get; set; }
}
