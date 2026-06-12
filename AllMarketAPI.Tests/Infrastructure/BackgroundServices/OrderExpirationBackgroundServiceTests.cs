using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.Categories.Models;
using AllMarket.Features.OrderItems.Models;
using AllMarket.Features.Orders.Models;
using AllMarket.Features.Products.Models;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.BackgroundServices;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AllMarketAPI.Tests.Infrastructure.BackgroundServices;

public class OrderExpirationBackgroundServiceTests
{
    [Fact]
    public async Task BackgroundService_ExpiresOrdersAndReleasesReservedStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SeedExpiredOrderAsync(database);

        var services = new ServiceCollection()
            .AddDbContext<AllMarketDbContext>(options =>
                options.UseSqlite(database.ConnectionString))
            .AddScoped<ICacheService, TestCacheService>()
            .BuildServiceProvider();
        await using var serviceProvider = services;
        var worker = new OrderExpirationBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OrderExpirationBackgroundService>.Instance);

        await worker.StartAsync(CancellationToken.None);

        var expired = await WaitForExpirationAsync(database);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(expired);
        database.Db.ChangeTracker.Clear();
        var order = await database.Db.Orders.SingleAsync();
        var product = await database.Db.Products.SingleAsync();
        Assert.Equal(Statuses.Expired, order.Status);
        Assert.Null(order.ReservationExpiresAt);
        Assert.Equal(0, product.ReservedStock);
    }

    private static async Task<bool> WaitForExpirationAsync(TestDatabase database)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            database.Db.ChangeTracker.Clear();

            if (await database.Db.Orders.AnyAsync(order =>
                    order.Status == Statuses.Expired))
            {
                return true;
            }

            await Task.Delay(25);
        }

        return false;
    }

    private static async Task SeedExpiredOrderAsync(TestDatabase database)
    {
        var user = new User
        {
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Address = "Test Address"
        };
        var category = new Category { Name = "Accessories" };
        var product = new Product
        {
            Name = "Mouse",
            Description = "Test product",
            Price = 50,
            Stock = 5,
            ReservedStock = 2,
            Category = category
        };
        var order = new Order
        {
            User = user,
            Status = Statuses.AwaitingPayment,
            TotalPrice = 100,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            Items =
            [
                new OrderItem
                {
                    Order = null!,
                    Product = product,
                    Quantity = 2,
                    PriceAtPurchase = 50
                }
            ]
        };

        database.Db.Orders.Add(order);
        await database.Db.SaveChangesAsync();
    }
}
