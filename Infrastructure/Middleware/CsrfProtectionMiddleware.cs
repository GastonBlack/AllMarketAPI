using System.Security.Cryptography;
using System.Text;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Security;

namespace AllMarket.Infrastructure.Middleware;

public class CsrfProtectionMiddleware(RequestDelegate next)
{
    private static readonly PathString StripeWebhookPath =
        new("/api/payments/stripe/webhook");

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresValidation(context.Request))
        {
            var cookieToken = context.Request.Cookies[CsrfConstants.CookieName];
            var headerToken = context.Request.Headers[CsrfConstants.HeaderName].ToString();

            if (!TokensMatch(cookieToken, headerToken))
                throw new ForbiddenException("Invalid CSRF token.");
        }

        await next(context);
    }

    private static bool RequiresValidation(HttpRequest request)
    {
        return request.Path != StripeWebhookPath &&
               request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private static bool TokensMatch(string? cookieToken, string headerToken)
    {
        if (string.IsNullOrWhiteSpace(cookieToken) ||
            string.IsNullOrWhiteSpace(headerToken))
        {
            return false;
        }

        var cookieBytes = Encoding.UTF8.GetBytes(cookieToken);
        var headerBytes = Encoding.UTF8.GetBytes(headerToken);

        return cookieBytes.Length == headerBytes.Length &&
               CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
    }
}
