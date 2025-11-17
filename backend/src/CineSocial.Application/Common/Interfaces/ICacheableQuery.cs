namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that should be cached
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Cache duration for this query. If null, uses default duration.
    /// </summary>
    TimeSpan? CacheDuration => null;

    /// <summary>
    /// Cache key prefix for this query type. Used for invalidation.
    /// </summary>
    string CacheKeyPrefix => GetType().Name;
}
