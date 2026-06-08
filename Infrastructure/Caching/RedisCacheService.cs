using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace AllMarket.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    // //////////////////////////////////////////
    // Configuration
    // //////////////////////////////////////////
    // Uses web defaults so cached JSON matches the API serialization style.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    // //////////////////////////////////////////
    // Injections
    // //////////////////////////////////////////
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // //////////////////////////////////////////
    // Getters
    // //////////////////////////////////////////
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);

            // Redis stores text, so the JSON is converted back to its original type.
            return cachedValue == null
                ? default
                : JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
        }
        catch (RedisException exception)
        {
            // Redis is optional: a cache failure must not break the API request.
            _logger.LogWarning(exception, "Redis read failed for key {CacheKey}.", key);
            return default;
        }
    }

    public async Task<string> GetVersionAsync(string key)
    {
        var version = await GetAsync<string>(key);

        if (version != null) return version;

        // The version groups related keys so they can be invalidated together.
        version = Guid.NewGuid().ToString("N");
        await SetAsync(key, version, TimeSpan.FromDays(1));

        return version;
    }

    // //////////////////////////////////////////
    // Modifiers
    // //////////////////////////////////////////
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            // Objects are serialized because Redis stores byte or string values.
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

    public Task RotateVersionAsync(string key)
    {
        // Existing keys become unreachable and expire naturally after their TTL.
        return SetAsync(key, Guid.NewGuid().ToString("N"), TimeSpan.FromDays(1));
    }
}
