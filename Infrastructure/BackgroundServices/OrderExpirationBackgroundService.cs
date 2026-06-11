using System.Data;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.BackgroundServices;

public class OrderExpirationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderExpirationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await ExpireOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to expire pending orders.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AllMarketDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var now = DateTime.UtcNow;

        var expiredOrderIds = await db.Orders
            .AsNoTracking()
            .Where(order =>
                order.Status == Statuses.AwaitingPayment &&
                order.ReservationExpiresAt.HasValue &&
                order.ReservationExpiresAt.Value <= now)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        var ordersExpired = 0;

        foreach (var orderId in expiredOrderIds)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var order = await db.Orders
                .Include(order => order.Items)
                .FirstOrDefaultAsync(order =>
                    order.Id == orderId &&
                    order.Status == Statuses.AwaitingPayment &&
                    order.ReservationExpiresAt.HasValue &&
                    order.ReservationExpiresAt.Value <= now,
                    cancellationToken);

            if (order == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            foreach (var item in order.Items)
            {
                var updatedRows = await db.Products
                    .Where(product =>
                        product.Id == item.ProductId &&
                        product.ReservedStock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            product => product.ReservedStock,
                            product => product.ReservedStock - item.Quantity),
                        cancellationToken);

                if (updatedRows != 1)
                    throw new InvalidOperationException(
                        $"Reserved stock could not be released for order {order.Id}.");
            }

            order.Status = Statuses.Expired;
            order.ReservationExpiresAt = null;

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
            ordersExpired++;
        }

        if (ordersExpired == 0) return;

        await cache.InvalidateProductsAsync();
        logger.LogInformation("Expired {OrderCount} pending orders.", ordersExpired);
    }
}
