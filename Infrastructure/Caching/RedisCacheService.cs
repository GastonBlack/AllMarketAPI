using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace AllMarket.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);

            return cachedValue == null
                ? default
                : JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis read failed for key {CacheKey}.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

            await _cache.SetStringAsync(
                key,
                serializedValue,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis write failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis removal failed for key {CacheKey}.", key);
        }
    }

    public async Task<string> GetVersionAsync(string key)
    {
        var version = await GetAsync<string>(key);

        if (version != null) return version;

        version = Guid.NewGuid().ToString("N");
        await SetAsync(key, version, TimeSpan.FromDays(1));

        return version;
    }

    public Task RotateVersionAsync(string key)
    {
        return SetAsync(key, Guid.NewGuid().ToString("N"), TimeSpan.FromDays(1));
    }
}
