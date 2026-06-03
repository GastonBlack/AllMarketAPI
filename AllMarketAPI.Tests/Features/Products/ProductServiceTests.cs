using AllMarket.Features.Categories.Models;
using AllMarket.Features.Products.Dto;
using AllMarket.Features.Products.Models;
using AllMarket.Features.Products.Services;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarketAPI.Tests.Features.Products;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAllProductsAsync_AppliesFiltersSortingAndPagination()
    {
        await using var db = CreateDbContext();
        await SeedProductsAsync(db);
        var service = new ProductServices(db);

        var result = await service.GetAllProductsAsync(new ProductQueryParams
        {
            Search = " MOUSE ",
            CategoryId = 1,
            MinPrice = 10,
            MaxPrice = 60,
            OnlyAvailable = true,
            SortBy = "popular",
            Page = 1,
            PageSize = 1
        });

        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Single(result.Items);
        Assert.Equal("Budget Mouse Pad", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllProductsAsync_UsesDiscountPriceForPriceFiltersAndSorting()
    {
        await using var db = CreateDbContext();
        await SeedProductsAsync(db);
        var service = new ProductServices(db);

        var result = await service.GetAllProductsAsync(new ProductQueryParams
        {
            MinPrice = 20,
            MaxPrice = 30,
            SortBy = "price_asc"
        });

        Assert.Single(result.Items);
        Assert.Equal("Premium Mouse", result.Items[0].Name);
        Assert.Equal(200, result.Items[0].Price);
        Assert.Equal(25, result.Items[0].DiscountPrice);
    }

    [Fact]
    public async Task GetAllProductsAsync_NormalizesInvalidPagingValues()
    {
        await using var db = CreateDbContext();
        var category = new Category { Id = 1, Name = "Accessories" };
        db.Categories.Add(category);

        for (var index = 1; index <= 101; index++)
        {
            db.Products.Add(new Product
            {
                Name = $"Product {index:000}",
                Description = "Test product",
                Price = index,
                Stock = 1,
                CategoryId = category.Id,
                Category = category
            });
        }

        await db.SaveChangesAsync();
        var service = new ProductServices(db);

        var result = await service.GetAllProductsAsync(new ProductQueryParams
        {
            Page = -5,
            PageSize = 500
        });

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(101, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(100, result.Items.Count);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetAllProductsAsync_RejectsInvalidSortBy()
    {
        await using var db = CreateDbContext();
        await SeedProductsAsync(db);
        var service = new ProductServices(db);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GetAllProductsAsync(new ProductQueryParams
            {
                SortBy = "rating_desc"
            }));
    }

    [Fact]
    public async Task GetAllProductsAsync_ExcludesInactiveProducts()
    {
        await using var db = CreateDbContext();
        await SeedProductsAsync(db);
        var service = new ProductServices(db);

        var result = await service.GetAllProductsAsync(new ProductQueryParams
        {
            Search = "disabled"
        });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    private static AllMarketDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AllMarketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AllMarketDbContext(options);
    }

    private static async Task SeedProductsAsync(AllMarketDbContext db)
    {
        var mice = new Category { Id = 1, Name = "Mice" };
        var keyboards = new Category { Id = 2, Name = "Keyboards" };

        db.Categories.AddRange(mice, keyboards);
        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Wireless Mouse",
                Description = "Bluetooth ergonomic accessory",
                Price = 50,
                Stock = 5,
                ReservedStock = 1,
                TotalSold = 20,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = mice.Id,
                Category = mice
            },
            new Product
            {
                Id = 2,
                Name = "Gaming Mouse",
                Description = "RGB gaming accessory",
                Price = 120,
                HasDiscount = true,
                DiscountPrice = 90,
                Stock = 0,
                TotalSold = 100,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = mice.Id,
                Category = mice
            },
            new Product
            {
                Id = 3,
                Name = "Mechanical Keyboard",
                Description = "Compact keyboard",
                Price = 80,
                Stock = 4,
                TotalSold = 30,
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = keyboards.Id,
                Category = keyboards
            },
            new Product
            {
                Id = 4,
                Name = "Budget Mouse Pad",
                Description = "Desk mat for mouse control",
                Price = 15,
                Stock = 10,
                TotalSold = 50,
                CreatedAt = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = mice.Id,
                Category = mice
            },
            new Product
            {
                Id = 5,
                Name = "Premium Mouse",
                Description = "High end mouse",
                Price = 200,
                HasDiscount = true,
                DiscountPrice = 25,
                Stock = 3,
                TotalSold = 10,
                CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = mice.Id,
                Category = mice
            },
            new Product
            {
                Id = 6,
                Name = "Disabled Mouse",
                Description = "disabled product",
                Price = 40,
                Stock = 10,
                IsActive = false,
                TotalSold = 500,
                CreatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                CategoryId = mice.Id,
                Category = mice
            });

        await db.SaveChangesAsync();
    }
}
