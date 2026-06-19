using System.Data;
using System.Net;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.Orders.Models;
using AllMarket.Features.Payments.Dto;
using AllMarket.Infrastructure.Caching;
using AllMarket.Infrastructure.Data;
using AllMarket.Infrastructure.Emails;
using AllMarket.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AllMarket.Features.Payments.Services;

public class PaymentService : IPaymentService
{
    // //////////////////////////////////////////
    // Inyections
    // //////////////////////////////////////////
    private readonly AllMarketDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;
    public PaymentService(
        AllMarketDbContext db,
        IConfiguration configuration,
        ICacheService cache,
        IEmailService emailService,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _configuration = configuration;
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
    }

    // //////////////////////////////////////////
    // Class Helpers
    // //////////////////////////////////////////
    private string GetRequiredConfiguration(string primaryKey, string fallbackKey)
    {
        var value = _configuration[primaryKey] ?? _configuration[fallbackKey];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{primaryKey} is not configured.");

        return value;
    }

    private string GetFrontendUrl()
    {
        return (_configuration["Frontend:Url"] ?? _configuration["FRONTEND_URL"] ?? "http://localhost:3000").TrimEnd('/');
    }

    private static long ToStripeAmount(decimal amount)
    {
        return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
    }

    private static List<SessionLineItemOptions> MapOrderItemsToLineItems(Order order)
    {
        return order.Items
            .Select(item =>
            {
                var images = string.IsNullOrWhiteSpace(item.Product.ImageUrl)
                    ? null
                    : new List<string> { item.Product.ImageUrl };

                return new SessionLineItemOptions
                {
                    Quantity = item.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = ToStripeAmount(item.PriceAtPurchase),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                            Images = images
                        }
                    }
                };
            })
            .ToList();
    }

    private Event BuildStripeEvent(string json, string signatureHeader)
    {
        var webhookSecret = GetRequiredConfiguration("Stripe:WebhookSecret", "STRIPE_WEBHOOK_SECRET");

        try
        {
            return EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
        }
        catch (StripeException exception)
        {
            throw new BadRequestException(exception.Message);
        }
    }

    private static int GetOrderIdFromSession(Session session)
    {
        if (session.Metadata.TryGetValue("orderId", out var orderIdValue) &&
            int.TryParse(orderIdValue, out var orderId))
        {
            return orderId;
        }

        throw new BadRequestException("Stripe session does not include a valid order id.");
    }

    private static int GetOrderIdFromRefund(Refund refund)
    {
        if (refund.Metadata.TryGetValue("orderId", out var orderIdValue) &&
            int.TryParse(orderIdValue, out var orderId))
        {
            return orderId;
        }

        throw new BadRequestException("Stripe refund does not include a valid order id.");
    }

    private string BuildOrderPaidEmailHtml(Order order)
    {
        var orderUrl = $"{GetFrontendUrl()}/orderHistory";
        var rows = string.Join(
            "",
            order.Items.Select(item =>
                $"""
                <tr>
                    <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.Product.Name)}</td>
                    <td style="padding:8px;border-bottom:1px solid #e5e7eb;text-align:center;">{item.Quantity}</td>
                    <td style="padding:8px;border-bottom:1px solid #e5e7eb;text-align:right;">${item.PriceAtPurchase:N2}</td>
                    <td style="padding:8px;border-bottom:1px solid #e5e7eb;text-align:right;">${item.Quantity * item.PriceAtPurchase:N2}</td>
                </tr>
                """));

        return $"""
            <h1 style="font-family:Arial,sans-serif;">Payment confirmed</h1>
            <p style="font-family:Arial,sans-serif;">Hi {WebUtility.HtmlEncode(order.User.FullName)}, your AllMarket order #{order.Id} was paid successfully.</p>
            <table style="border-collapse:collapse;width:100%;font-family:Arial,sans-serif;">
                <thead>
                    <tr>
                        <th style="padding:8px;border-bottom:1px solid #d4d4d8;text-align:left;">Product</th>
                        <th style="padding:8px;border-bottom:1px solid #d4d4d8;text-align:center;">Qty</th>
                        <th style="padding:8px;border-bottom:1px solid #d4d4d8;text-align:right;">Price</th>
                        <th style="padding:8px;border-bottom:1px solid #d4d4d8;text-align:right;">Subtotal</th>
                    </tr>
                </thead>
                <tbody>{rows}</tbody>
            </table>
            <p style="font-family:Arial,sans-serif;font-size:18px;"><strong>Total: ${order.TotalPrice:N2}</strong></p>
            <p style="font-family:Arial,sans-serif;"><a href="{orderUrl}">View your order history</a></p>
            <p style="font-family:Arial,sans-serif;color:#71717a;">This is a portfolio project, not a real store.</p>
            """;
    }

    private async Task SendOrderPaidEmailAsync(Order order)
    {
        try
        {
            await _emailService.SendAsync(
                order.User.Email,
                order.User.FullName,
                $"AllMarket order #{order.Id} confirmed",
                BuildOrderPaidEmailHtml(order));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Order paid email could not be sent for order {OrderId}.",
                order.Id);
        }
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(int orderId, int userId)
    {
        if (orderId <= 0) throw new BadRequestException("Invalid order id.");

        StripeConfiguration.ApiKey = GetRequiredConfiguration("Stripe:SecretKey", "STRIPE_SECRET_KEY");

        var order = await _db.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == orderId && order.UserId == userId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != Statuses.AwaitingPayment)
            throw new ConflictException("Only orders awaiting payment can be paid.");

        if (order.ReservationExpiresAt.HasValue && order.ReservationExpiresAt.Value <= DateTime.UtcNow)
            throw new ConflictException("Order reservation has expired.");

        var frontendUrl = GetFrontendUrl();
        var metadata = new Dictionary<string, string>
        {
            ["orderId"] = order.Id.ToString(),
            ["userId"] = order.UserId.ToString()
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = order.User.Email,
            ClientReferenceId = order.Id.ToString(),
            SuccessUrl = $"{frontendUrl}/orderHistory?payment=success&orderId={order.Id}",
            CancelUrl = $"{frontendUrl}/cart?payment=cancelled&orderId={order.Id}",
            LineItems = MapOrderItemsToLineItems(order),
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = metadata
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        if (string.IsNullOrWhiteSpace(session.Url))
            throw new InvalidOperationException("Stripe checkout session URL was not generated.");

        return new CheckoutSessionResponseDto
        {
            CheckoutUrl = session.Url,
            SessionId = session.Id
        };
    }

    public async Task HandleStripeWebhookAsync(string json, string signatureHeader)
    {
        var stripeEvent = BuildStripeEvent(json, signatureHeader);

        if (stripeEvent.Type == "checkout.session.completed")
        {
            if (stripeEvent.Data.Object is not Session session)
                throw new BadRequestException("Invalid Stripe checkout session event.");

            var orderId = GetOrderIdFromSession(session);
            await MarkOrderAsPaidAsync(orderId, session.PaymentIntentId);
            return;
        }

        if (stripeEvent.Type is "refund.created" or "refund.updated" or "refund.failed")
        {
            if (stripeEvent.Data.Object is not Refund refund)
                throw new BadRequestException("Invalid Stripe refund event.");

            await HandleRefundAsync(refund);
        }
    }

    public async Task RefundOrderAsync(int orderId)
    {
        if (orderId <= 0) throw new BadRequestException("Invalid order id.");

        StripeConfiguration.ApiKey = GetRequiredConfiguration("Stripe:SecretKey", "STRIPE_SECRET_KEY");

        await using (var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var order = await _db.Orders
                .FirstOrDefaultAsync(order => order.Id == orderId)
                ?? throw new NotFoundException("Order not found.");

            if (order.Status is Statuses.Refunding or Statuses.Refunded)
            {
                await transaction.CommitAsync();
                return;
            }

            if (order.Status is not Statuses.Paid and not Statuses.Preparing)
                throw new ConflictException("Only paid or preparing orders can be refunded.");

            if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
                throw new ConflictException("This order does not have a Stripe payment reference.");

            order.PreRefundStatus = order.Status;
            order.Status = Statuses.Refunding;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        _db.ChangeTracker.Clear();

        var refundOrder = await _db.Orders
            .AsNoTracking()
            .FirstAsync(order => order.Id == orderId);

        var options = new RefundCreateOptions
        {
            PaymentIntent = refundOrder.StripePaymentIntentId,
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = refundOrder.Id.ToString()
            }
        };
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"order-refund-{refundOrder.Id}"
        };

        Refund refund;
        try
        {
            refund = await new RefundService().CreateAsync(options, requestOptions);
        }
        catch (StripeException)
        {
            await RestoreOrderAfterRefundFailureAsync(orderId);
            throw new ConflictException("Stripe could not create the refund.");
        }

        await HandleRefundAsync(refund);
    }

    private async Task MarkOrderAsPaidAsync(int orderId, string? paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            throw new BadRequestException("Stripe session does not include a payment intent.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await _db.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status == Statuses.Paid)
        {
            if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            {
                order.StripePaymentIntentId = paymentIntentId;
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return;
        }

        if (order.Status != Statuses.AwaitingPayment)
            throw new ConflictException("Order is not awaiting payment.");

        foreach (var item in order.Items)
        {
            var updatedRows = await _db.Products
                .Where(product => product.Id == item.ProductId && product.ReservedStock >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.Stock, product => product.Stock - item.Quantity)
                    .SetProperty(product => product.ReservedStock, product => product.ReservedStock - item.Quantity)
                    .SetProperty(product => product.TotalSold, product => product.TotalSold + item.Quantity));

            if (updatedRows != 1) throw new ConflictException("Reserved stock is no longer available.");
        }

        order.Status = Statuses.Paid;
        order.StripePaymentIntentId = paymentIntentId;
        order.ReservationExpiresAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await _cache.InvalidateProductsAsync();
        await SendOrderPaidEmailAsync(order);
    }

    private async Task HandleRefundAsync(Refund refund)
    {
        var orderId = GetOrderIdFromRefund(refund);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await _db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status == Statuses.Refunded)
        {
            await transaction.CommitAsync();
            return;
        }

        if (refund.Status is "failed" or "canceled")
        {
            if (order.Status == Statuses.Refunding && !string.IsNullOrWhiteSpace(order.PreRefundStatus))
            {
                order.Status = order.PreRefundStatus;
                order.PreRefundStatus = null;
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return;
        }

        if (order.Status is not Statuses.Paid and not Statuses.Preparing and not Statuses.Refunding)
            throw new ConflictException("Order cannot be refunded in its current status.");

        order.PreRefundStatus ??= order.Status;
        order.StripeRefundId = refund.Id;

        if (refund.Status != "succeeded")
        {
            order.Status = Statuses.Refunding;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return;
        }

        foreach (var item in order.Items)
        {
            var updatedRows = await _db.Products
                .Where(product => product.Id == item.ProductId && product.TotalSold >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.Stock, product => product.Stock + item.Quantity)
                    .SetProperty(product => product.TotalSold, product => product.TotalSold - item.Quantity));

            if (updatedRows != 1) throw new ConflictException("Product stock could not be restored.");
        }

        order.Status = Statuses.Refunded;
        order.PreRefundStatus = null;
        order.RefundedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await _cache.InvalidateProductsAsync();
    }

    private async Task RestoreOrderAfterRefundFailureAsync(int orderId)
    {
        _db.ChangeTracker.Clear();

        var order = await _db.Orders
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != Statuses.Refunding || string.IsNullOrWhiteSpace(order.PreRefundStatus))
            return;

        order.Status = order.PreRefundStatus;
        order.PreRefundStatus = null;
        await _db.SaveChangesAsync();
    }
}
