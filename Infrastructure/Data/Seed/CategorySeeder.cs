using AllMarket.Features.Categories.Models;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Data.Seed;

public static class CategorySeeder
{
    public static string[] GetCategoryNames()
    {
        return new[]
        {
            "Headphones",
            "Consoles",
            "Phones",
            "Graphics Cards"
        };
    }

    public static async Task SeedAsync(AllMarketDbContext db)
    {
        var categoryNames = GetCategoryNames();

        var existingCategoryNames = await db.Categories
            .Where(category => categoryNames.Contains(category.Name))
            .Select(category => category.Name)
            .ToListAsync();

        var categoriesToCreate = categoryNames
            .Where(categoryName => !existingCategoryNames.Contains(categoryName))
            .Select(categoryName => new Category { Name = categoryName })
            .ToList();

        if (categoriesToCreate.Count == 0) return;

        await db.Categories.AddRangeAsync(categoriesToCreate);
        await db.SaveChangesAsync();
    }
}
