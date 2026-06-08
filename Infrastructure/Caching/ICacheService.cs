namespace AllMarket.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<string> GetVersionAsync(string key);
    Task RemoveAsync(string key);
    Task RotateVersionAsync(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
}
