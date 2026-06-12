using System.Collections.Concurrent;
using AllMarket.Infrastructure.Caching;

namespace AllMarketAPI.Tests;

public class TestCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _values = new();

    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(
            _values.TryGetValue(key, out var value) ? (T)value : default);
    }

    public async Task<string> GetVersionAsync(string key)
    {
        var version = await GetAsync<string>(key);

        if (version != null) return version;

        version = Guid.NewGuid().ToString("N");
        await SetAsync(key, version, TimeSpan.FromDays(1));

        return version;
    }

    public Task RemoveAsync(string key)
    {
        _values.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RotateVersionAsync(string key)
    {
        _values[key] = Guid.NewGuid().ToString("N");
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        _values[key] = value!;
        return Task.CompletedTask;
    }
}
