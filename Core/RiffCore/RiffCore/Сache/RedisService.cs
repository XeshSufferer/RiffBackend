using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;


namespace RiffCore.Сache;

public class RedisService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _defaultOptions;

    public RedisService(IDistributedCache cache)
    {
        _cache = cache;
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = expiry.HasValue 
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }
            : _defaultOptions;

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        await _cache.SetAsync(key, bytes, options);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes == null) return default;

        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var bytes = await _cache.GetAsync(key);
        return bytes != null;
    }
}