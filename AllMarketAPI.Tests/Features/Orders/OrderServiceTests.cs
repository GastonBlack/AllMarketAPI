using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.Categories.Models;
using AllMarket.Features.OrderItems.Dto;
using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Orders.Services;
using AllMarket.Features.Products.Models;
using AllMarket.Features.Users.Models;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarketAPI.Tests.Features.Orders;

public class OrderServiceTests
{
    [Fact]
    public async Task CheckoutAsync_ReservesStockAndUsesCurrentPurchasePrice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await SeedAsync(database);
        var service = new OrderService(database.Db, new TestCacheService());
        var beforeCheckout = DateTime.UtcNow;

        var order = await service.CheckoutAsync(new CreateOrderDto
        {
            Items =
            [
                new CreateOrderItemDto { ProductId = product.Id, Quantity = 1 },
                new CreateOrderItemDto { ProductId = product.Id, Quantity = 2 }
            ]
        }, user.Id);

        database.Db.ChangeTracker.Clear();
        var storedProduct = await database.Db.Products.SingleAsync();

        Assert.Equal(Statuses.AwaitingPayment, order.Status);
        Assert.Equal(3, storedProduct.ReservedStock);
        Assert.Equal(75, order.TotalPrice);
        Assert.Single(order.Items);
        Assert.Equal(25, order.Items[0].PriceAtPurchase);
        Assert.InRange(
            order.ReservationExpiresAt!.Value,
            beforeCheckout.AddMinutes(14),
            beforeCheckout.AddMinutes(16));
    }

    [Fact]
    public async Task CheckoutAsync_WhenStockIsInsufficient_DoesNotCreateOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (user, product) = await SeedAsync(database);
        var service = new OrderService(database.Db, new TestCacheService());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CheckoutAsync(new CreateOrderDto
            {
                Items =
                [
                    new CreateOrderItemDto
                    {
                        ProductId = product.Id,
                        Quantity = 6
                    }
                ]
            }, user.Id));

        database.Db.ChangeTracker.Clear();
        Assert.Empty(await database.Db.Orders.ToListAsync());
        Assert.Equal(0, (await database.Db.Products.SingleAsync()).ReservedStock);
    }

    private static async Task<(User User, Product Product)> SeedAsync(
        TestDatabase database)
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
            Name = "Premium Mouse",
            Description = "Test product",
            Price = 100,
            HasDiscount = true,
            DiscountPrice = 25,
            Stock = 5,
            Category = category
        };

        database.Db.AddRange(user, product);
        await database.Db.SaveChangesAsync();

        return (user, product);
    }
}
