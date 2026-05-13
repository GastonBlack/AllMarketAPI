using System.ComponentModel.DataAnnotations;

namespace AllMarket.Features.Categories.Dto;

public class UpdateCategoryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Category id must be greater than 0.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 60 characters.")]
    public required string Name { get; set; }
}
