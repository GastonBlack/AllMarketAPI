using System.Data;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.OrderItems.Dto;
using AllMarket.Features.OrderItems.Models;
using AllMarket.Features.Orders.Dto;
using AllMarket.Features.Orders.Models;
using AllMarket.Features.Products.Models;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Orders.Services;

public class OrderService : IOrderService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    public OrderService(AllMarketDbContext db)
    {
        _db = db;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private static decimal GetProductPurchasePrice(Product product)
    {
        return product.HasDiscount && product.DiscountPrice.HasValue
            ? product.DiscountPrice.Value
            : product.Price;
    }

    private static OrderResponseDto MapToOrderResponseDto(Order order, IReadOnlyDictionary<int, Product> productsById)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            CreatedAt = order.CreatedAt,
            ReservationExpiresAt = order.ReservationExpiresAt,
            Items = order.Items
                .Select(item => new OrderItemResponseDto
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    ProductId = item.ProductId,
                    ProductName = productsById[item.ProductId].Name,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.PriceAtPurchase,
                    Subtotal = item.Quantity * item.PriceAtPurchase
                })
                .ToList()
        };
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<OrderResponseDto> CheckoutAsync(CreateOrderDto dto, int userId)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");
        if (dto.Items.Count == 0) throw new BadRequestException("Order must contain at least one item.");

        var requestedItems = dto.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (requestedItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            throw new BadRequestException("Invalid order items.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive);

        if (!userExists) throw new NotFoundException("User not found.");

        var productIds = requestedItems
            .Select(item => item.ProductId)
            .ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products were not found.");

        var productsById = products.ToDictionary(product => product.Id);

        foreach (var requestedItem in requestedItems)
        {
            var product = productsById[requestedItem.ProductId];

            if (!product.IsActive)
                throw new ConflictException($"{product.Name} is not available.");

            var availableStock = product.Stock - product.ReservedStock;

            if (availableStock < requestedItem.Quantity)
                throw new ConflictException($"{product.Name} does not have enough stock.");

            // Atomically reserves stock so concurrent checkouts cannot oversell.
            var updatedRows = await _db.Products
                .Where(product =>
                    product.Id == requestedItem.ProductId &&
                    product.IsActive &&
                    product.Stock - product.ReservedStock >= requestedItem.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        product => product.ReservedStock,
                        product => product.ReservedStock + requestedItem.Quantity));

            if (updatedRows != 1)
                throw new ConflictException($"{product.Name} does not have enough stock.");
        }

        var order = new Order
        {
            UserId = userId,
            User = null!,
            Status = Statuses.AwaitingPayment,
            CreatedAt = DateTime.UtcNow,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Items = requestedItems
                .Select(item =>
                {
                    var product = productsById[item.ProductId];
                    var priceAtPurchase = GetProductPurchasePrice(product);

                    return new OrderItem
                    {
                        Order = null!,
                        ProductId = product.Id,
                        Product = null!,
                        Quantity = item.Quantity,
                        PriceAtPurchase = priceAtPurchase
                    };
                })
                .ToList()
        };

        order.TotalPrice = order.Items.Sum(item => item.Quantity * item.PriceAtPurchase);

        await _db.Orders.AddAsync(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapToOrderResponseDto(order, productsById);
    }
}
