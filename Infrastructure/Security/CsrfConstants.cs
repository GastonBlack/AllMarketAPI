namespace AllMarket.Infrastructure.Security;

public static class CsrfConstants
{
    public const string CookieName = "csrf_token";
    public const string HeaderName = "X-CSRF-Token";
}
