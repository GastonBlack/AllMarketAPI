using System.Globalization;
using AllMarket.Features.Products.Dto;

namespace AllMarket.Infrastructure.Caching;

public static class CacheKeys
{
    public const string Categories = "categories:all";
    public const string ProductDetailsVersion = "products:details:version";
    public const string PopularProductsVersion = "products:popular:version";

    public static string Product(int productId, string version)
    {
        return $"products:details:{version}:{productId}";
    }

    public static string PopularProducts(
        ProductQueryParams queryParams,
        int page,
        int pageSize,
        string version)
    {
        var search = Uri.EscapeDataString(
            queryParams.Search?.Trim().ToLowerInvariant() ?? string.Empty);

        return string.Join(
            ':',
            "products",
            "popular",
            version,
            page,
            pageSize,
            search,
            queryParams.CategoryId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            queryParams.MinPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            queryParams.MaxPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            queryParams.OnlyAvailable);
    }
}
