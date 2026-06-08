namespace AllMarket.Infrastructure.Caching;

public static class CacheInvalidationExtensions
{
    public static Task InvalidateProductsAsync(this ICacheService cache)
    {
        return Task.WhenAll(
            cache.RotateVersionAsync(CacheKeys.ProductDetailsVersion),
            cache.RotateVersionAsync(CacheKeys.PopularProductsVersion));
    }
}
