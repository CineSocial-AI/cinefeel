using CineSocial.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace CineSocial.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private readonly IFusionCache _fusionCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheService> _logger;

    public CacheService(
        IFusionCache fusionCache,
        IConnectionMultiplexer redis,
        ILogger<CacheService> logger)
    {
        _fusionCache = fusionCache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = await _fusionCache.TryGetAsync<T>(key, token: cancellationToken);

            if (result.HasValue)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return result.Value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new FusionCacheEntryOptions
            {
                Duration = expiration ?? TimeSpan.FromMinutes(10),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
            };

            return await _fusionCache.GetOrSetAsync<T>(
                key,
                async (ctx, ct) => await factory(ct),
                options,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // Fallback to factory if cache fails
            return await factory(cancellationToken);
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new FusionCacheEntryOptions
            {
                Duration = expiration ?? TimeSpan.FromMinutes(10)
            };

            await _fusionCache.SetAsync(key, value, options, cancellationToken);
            _logger.LogDebug("Cached value set for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _fusionCache.RemoveAsync(key, token: cancellationToken);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cached values with prefix: {Prefix}", keys.Length, prefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by prefix: {Prefix}", prefix);
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            // FusionCache doesn't natively support tags, so we use a custom implementation
            // We store tag->key mappings in Redis sets
            var db = _redis.GetDatabase();
            var tagKey = $"tag:{tag}";

            var keys = await db.SetMembersAsync(tagKey);

            if (keys.Length > 0)
            {
                foreach (var key in keys)
                {
                    await _fusionCache.RemoveAsync(key.ToString()!, token: cancellationToken);
                }

                await db.KeyDeleteAsync(tagKey);
                _logger.LogDebug("Removed {Count} cached values with tag: {Tag}", keys.Length, tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by tag: {Tag}", tag);
        }
    }
}
