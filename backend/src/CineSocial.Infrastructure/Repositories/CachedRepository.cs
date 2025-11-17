using System.Linq.Expressions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;
using CineSocial.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace CineSocial.Infrastructure.Repositories;

/// <summary>
/// Decorator pattern for caching repository operations
/// Wraps an existing IRepository implementation and adds caching layer
/// </summary>
public class CachedRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IRepository<T> _innerRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedRepository<T>> _logger;
    private readonly string _entityName;

    public CachedRepository(
        IRepository<T> innerRepository,
        ICacheService cacheService,
        ILogger<CachedRepository<T>> logger)
    {
        _innerRepository = innerRepository;
        _cacheService = cacheService;
        _logger = logger;
        _entityName = typeof(T).Name.ToLowerInvariant();
    }

    private string GetCacheKey(Guid id) => $"{_entityName}:id:{id}";
    private string GetCacheKeyPrefix() => $"{_entityName}:";
    private string GetCacheKeyForQuery(string queryIdentifier) => $"{_entityName}:query:{queryIdentifier}";

    private TimeSpan GetCacheDuration()
    {
        // Determine cache duration based on entity type
        return _entityName switch
        {
            "genre" or "country" or "language" or "keyword" => FusionCacheConfiguration.CacheDurations.ReferenceData,
            "movieentity" or "person" => FusionCacheConfiguration.CacheDurations.MoviesAndPeople,
            "comment" or "rate" or "reaction" => FusionCacheConfiguration.CacheDurations.CommentsAndRates,
            "appuser" or "movielist" => FusionCacheConfiguration.CacheDurations.UserData,
            _ => TimeSpan.FromMinutes(10)
        };
    }

    // Passthrough QueryN methods (no caching for IQueryable)
    public IQueryable<T> Query() => _innerRepository.Query();
    public IQueryable<T> QueryNoTracking() => _innerRepository.QueryNoTracking();

    // Cached GetByIdAsync
    public async Task<Result<T>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(id);

        try
        {
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async ct => await _innerRepository.GetByIdAsync(id, ct),
                GetCacheDuration(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cached GetByIdAsync for {EntityName} with ID: {Id}", _entityName, id);
            // Fallback to direct repository call
            return await _innerRepository.GetByIdAsync(id, cancellationToken);
        }
    }

    public Task<Result<T>> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        // Skip caching for includes (complex scenarios)
        return _innerRepository.GetByIdAsync(id, includes);
    }

    // Passthrough for predicate-based queries (harder to cache)
    public Task<Result<T>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _innerRepository.FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<Result<T>> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _innerRepository.SingleOrDefaultAsync(predicate, cancellationToken);

    // Cached GetAllAsync
    public async Task<Result<List<T>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_entityName}:all";

        try
        {
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async ct => await _innerRepository.GetAllAsync(ct),
                GetCacheDuration(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cached GetAllAsync for {EntityName}", _entityName);
            return await _innerRepository.GetAllAsync(cancellationToken);
        }
    }

    public Task<Result<List<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _innerRepository.FindAsync(predicate, cancellationToken);

    // Pagination - skip caching (too many variations)
    public Task<PagedResult<List<T>>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => _innerRepository.GetPagedAsync(page, pageSize, cancellationToken);

    public Task<PagedResult<List<T>>> GetPagedAsync(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken cancellationToken = default)
        => _innerRepository.GetPagedAsync(predicate, page, pageSize, cancellationToken);

    // Existence checks - passthrough
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _innerRepository.ExistsAsync(id, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _innerRepository.AnyAsync(predicate, cancellationToken);

    // Count - passthrough
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _innerRepository.CountAsync(cancellationToken);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _innerRepository.CountAsync(predicate, cancellationToken);

    // Mutations with cache invalidation
    public async Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(entity, cancellationToken);

        if (result.IsSuccess)
        {
            // Invalidate cache for this entity type
            await InvalidateCacheAsync();
        }

        return result;
    }

    public async Task<Result<List<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddRangeAsync(entities, cancellationToken);

        if (result.IsSuccess)
        {
            await InvalidateCacheAsync();
        }

        return result;
    }

    public Result<T> Update(T entity)
    {
        var result = _innerRepository.Update(entity);

        if (result.IsSuccess)
        {
            // Invalidate specific entity cache (fire and forget)
            Task.Run(async () =>
            {
                await _cacheService.RemoveAsync(GetCacheKey(entity.Id));
                await InvalidateCacheAsync();
            });
        }

        return result;
    }

    public Result<List<T>> UpdateRange(IEnumerable<T> entities)
    {
        var result = _innerRepository.UpdateRange(entities);

        if (result.IsSuccess)
        {
            Task.Run(async () => await InvalidateCacheAsync());
        }

        return result;
    }

    public Result Delete(T entity)
    {
        var result = _innerRepository.Delete(entity);

        if (result.IsSuccess)
        {
            Task.Run(async () =>
            {
                await _cacheService.RemoveAsync(GetCacheKey(entity.Id));
                await InvalidateCacheAsync();
            });
        }

        return result;
    }

    public Result DeleteRange(IEnumerable<T> entities)
    {
        var result = _innerRepository.DeleteRange(entities);

        if (result.IsSuccess)
        {
            Task.Run(async () => await InvalidateCacheAsync());
        }

        return result;
    }

    public Result HardDelete(T entity)
    {
        var result = _innerRepository.HardDelete(entity);

        if (result.IsSuccess)
        {
            Task.Run(async () =>
            {
                await _cacheService.RemoveAsync(GetCacheKey(entity.Id));
                await InvalidateCacheAsync();
            });
        }

        return result;
    }

    public Result HardDeleteRange(IEnumerable<T> entities)
    {
        var result = _innerRepository.HardDeleteRange(entities);

        if (result.IsSuccess)
        {
            Task.Run(async () => await InvalidateCacheAsync());
        }

        return result;
    }

    private async Task InvalidateCacheAsync()
    {
        try
        {
            await _cacheService.RemoveByPrefixAsync(GetCacheKeyPrefix());
            _logger.LogDebug("Invalidated cache for entity type: {EntityName}", _entityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for entity type: {EntityName}", _entityName);
        }
    }
}
