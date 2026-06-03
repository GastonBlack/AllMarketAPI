using System.Data;
using AllMarket.Constants.OrderStatuses;
using AllMarket.Features.Orders.Models;
using AllMarket.Features.Payments.Dto;
using AllMarket.Infrastructure.Data;
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
    public PaymentService(AllMarketDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
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

        if (stripeEvent.Type != "checkout.session.completed") return;

        if (stripeEvent.Data.Object is not Session session)
            throw new BadRequestException("Invalid Stripe checkout session event.");

        var orderId = GetOrderIdFromSession(session);
        await MarkOrderAsPaidAsync(orderId);
    }

    private async Task MarkOrderAsPaidAsync(int orderId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await _db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status == Statuses.Paid)
        {
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
        order.ReservationExpiresAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
