using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Middleware;
using AllMarket.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace AllMarketAPI.Tests.Infrastructure.Security;

public class CsrfProtectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PostWithoutMatchingTokens_IsRejected()
    {
        var middleware = new CsrfProtectionMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("POST", "/api/orders/checkout");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_PostWithMatchingTokens_ContinuesPipeline()
    {
        var nextCalled = false;
        var middleware = new CsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/api/orders/checkout");
        context.Request.Headers.Cookie = $"{CsrfConstants.CookieName}=test-token";
        context.Request.Headers[CsrfConstants.HeaderName] = "test-token";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_StripeWebhook_DoesNotRequireCsrfToken()
    {
        var nextCalled = false;
        var middleware = new CsrfProtectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/api/payments/stripe/webhook");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }
}
