using System.Data;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.Admin.Orders.Dto;
using AllMarket.Features.Orders.Models;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Responses;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Features.Admin.Orders.Services;

public class AdminOrderService : IAdminOrderService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly ICacheService _cache;
    public AdminOrderService(AllMarketDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private static readonly HashSet<string> ValidStatuses =
    [
        Statuses.AwaitingPayment,
        Statuses.Paid,
        Statuses.Preparing,
        Statuses.Shipped,
        Statuses.Delivered,
        Statuses.Cancelled,
        Statuses.Expired,
        Statuses.Refunding,
        Statuses.Refunded
    ];

    private static bool CanChangeStatus(string status)
    {
        return status is Statuses.AwaitingPayment
            or Statuses.Paid
            or Statuses.Preparing
            or Statuses.Shipped;
    }

    private static bool IsValidTransition(string currentStatus, string nextStatus)
    {
        if (currentStatus == nextStatus) return true;

        return currentStatus switch
        {
            Statuses.AwaitingPayment => nextStatus is Statuses.Paid or Statuses.Cancelled or Statuses.Expired,
            Statuses.Paid => nextStatus == Statuses.Preparing,
            Statuses.Preparing => nextStatus == Statuses.Shipped,
            Statuses.Shipped => nextStatus == Statuses.Delivered,
            _ => false
        };
    }

    private static AdminOrderResponseDto MapToListDto(Order order)
    {
        return new AdminOrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserFullName = order.User.FullName,
            UserEmail = order.User.Email,
            CreatedAt = order.CreatedAt,
            ProductCount = order.Items.Sum(item => item.Quantity),
            Status = order.Status,
            TotalPrice = order.TotalPrice
        };
    }

    private static AdminOrderDetailResponseDto MapToDetailDto(Order order)
    {
        return new AdminOrderDetailResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserFullName = order.User.FullName,
            UserEmail = order.User.Email,
            CreatedAt = order.CreatedAt,
            ReservationExpiresAt = order.ReservationExpiresAt,
            Status = order.Status,
            CanChangeStatus = CanChangeStatus(order.Status),
            CanRefund = order.Status is Statuses.Paid or Statuses.Preparing &&
                        !string.IsNullOrWhiteSpace(order.StripePaymentIntentId),
            TotalPrice = order.TotalPrice,
            Items = order.Items
                .Select(item => new AdminOrderItemResponseDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.PriceAtPurchase,
                    Subtotal = item.Quantity * item.PriceAtPurchase
                })
                .ToList()
        };
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<PaginatedResponse<AdminOrderResponseDto>> GetOrdersAsync(AdminOrderQueryParams queryParams)
    {
        queryParams ??= new AdminOrderQueryParams();

        var page = queryParams.Page < 1 ? AdminOrderQueryParams.DefaultPage : queryParams.Page;
        var pageSize = queryParams.PageSize switch
        {
            < 1 => AdminOrderQueryParams.DefaultPageSize,
            > AdminOrderQueryParams.MaxPageSize => AdminOrderQueryParams.MaxPageSize,
            _ => queryParams.PageSize
        };

        var query = _db.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Items)
            .AsQueryable();

        var search = queryParams.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchIsId = int.TryParse(search, out var orderId);

            query = query.Where(order =>
                (searchIsId && order.Id == orderId) ||
                order.User.FullName.ToLower().Contains(search) ||
                order.User.Email.ToLower().Contains(search));
        }

        if (queryParams.OrderId.HasValue)
            query = query.Where(order => order.Id == queryParams.OrderId.Value);

        var userName = queryParams.UserName?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(order => order.User.FullName.ToLower().Contains(userName));

        var userEmail = queryParams.UserEmail?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(userEmail))
            query = query.Where(order => order.User.Email.ToLower().Contains(userEmail));

        var status = queryParams.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ValidStatuses.Contains(status))
                throw new BadRequestException("Invalid order status.");

            query = query.Where(order => order.Status == status);
        }

        if (queryParams.FromDate.HasValue)
            query = query.Where(order => order.CreatedAt >= queryParams.FromDate.Value);

        if (queryParams.ToDate.HasValue)
            query = query.Where(order => order.CreatedAt <= queryParams.ToDate.Value);

        query = query.OrderBy(order => order.CreatedAt).ThenBy(order => order.Id);

        var totalItems = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<AdminOrderResponseDto>(
            orders.Select(MapToListDto).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<AdminOrderDetailResponseDto> GetOrderByIdAsync(int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        return MapToDetailDto(order);
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<AdminOrderDetailResponseDto> UpdateOrderStatusAsync(int orderId, AdminUpdateOrderStatusDto dto)
    {
        if (dto == null) throw new BadRequestException("Invalid data.");

        var nextStatus = dto.Status.Trim();
        if (!ValidStatuses.Contains(nextStatus)) throw new BadRequestException("Invalid order status.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await _db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (!IsValidTransition(order.Status, nextStatus))
            throw new BadRequestException("Invalid order status transition.");

        if (order.Status == nextStatus)
        {
            await transaction.CommitAsync();
            return await GetOrderByIdAsync(order.Id);
        }

        var productsChanged =
            order.Status == Statuses.AwaitingPayment &&
            nextStatus is Statuses.Paid or Statuses.Cancelled or Statuses.Expired;

        foreach (var item in order.Items)
        {
            if (order.Status == Statuses.AwaitingPayment && nextStatus == Statuses.Paid)
            {
                var updatedRows = await _db.Products
                    .Where(product => product.Id == item.ProductId && product.ReservedStock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.Stock, product => product.Stock - item.Quantity)
                        .SetProperty(product => product.ReservedStock, product => product.ReservedStock - item.Quantity)
                        .SetProperty(product => product.TotalSold, product => product.TotalSold + item.Quantity));

                if (updatedRows != 1) throw new ConflictException("Reserved stock is no longer available.");
            }

            if (order.Status == Statuses.AwaitingPayment && nextStatus is Statuses.Cancelled or Statuses.Expired)
            {
                var updatedRows = await _db.Products
                    .Where(product => product.Id == item.ProductId && product.ReservedStock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.ReservedStock, product => product.ReservedStock - item.Quantity));

                if (updatedRows != 1) throw new ConflictException("Reserved stock is no longer available.");
            }
        }

        order.Status = nextStatus;
        if (nextStatus is Statuses.Paid or Statuses.Cancelled or Statuses.Expired)
            order.ReservationExpiresAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        if (productsChanged)
            await _cache.InvalidateProductsAsync();

        return await GetOrderByIdAsync(order.Id);
    }
}
