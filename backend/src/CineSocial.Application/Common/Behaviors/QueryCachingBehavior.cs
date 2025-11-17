using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically caches queries implementing ICacheableQuery
/// </summary>
public class QueryCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        ICacheService cacheService,
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache queries that implement ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        // Only cache successful Result<T> or PagedResult<T> responses
        if (!IsResultType(typeof(TResponse)))
        {
            _logger.LogWarning("Query {QueryType} implements ICacheableQuery but response type {ResponseType} is not cacheable",
                typeof(TRequest).Name, typeof(TResponse).Name);
            return await next();
        }

        try
        {
            // Generate cache key from query properties
            var cacheKey = GenerateCacheKey(request, cacheableQuery);

            // Get cache duration
            var cacheDuration = cacheableQuery.CacheDuration ?? TimeSpan.FromMinutes(15);

            _logger.LogDebug("Attempting to get cached result for query {QueryType} with key {CacheKey}",
                typeof(TRequest).Name, cacheKey);

            // Try to get from cache or execute query
            var result = await _cacheService.GetOrSetAsync(
                cacheKey,
                async ct => await next(),
                cacheDuration,
                cancellationToken);

            if (result != null)
            {
                _logger.LogDebug("Cache hit for query {QueryType}", typeof(TRequest).Name);
            }

            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in QueryCachingBehavior for query {QueryType}", typeof(TRequest).Name);
            // Fallback to executing query without cache
            return await next();
        }
    }

    private static string GenerateCacheKey(TRequest request, ICacheableQuery cacheableQuery)
    {
        // Use SearchCacheKeyGenerator for search queries
        var queryType = request.GetType();
        var queryName = queryType.Name;

        // For search queries with Query, Page, PageSize properties
        var queryProp = queryType.GetProperty("Query");
        var pageProp = queryType.GetProperty("Page");
        var pageSizeProp = queryType.GetProperty("PageSize");

        if (queryProp != null && pageProp != null && pageSizeProp != null)
        {
            var query = queryProp.GetValue(request)?.ToString() ?? string.Empty;
            var page = (int?)pageProp.GetValue(request) ?? 1;
            var pageSize = (int?)pageSizeProp.GetValue(request) ?? 20;

            return $"query:{queryName.ToLowerInvariant()}:{NormalizeQuery(query)}:p{page}:s{pageSize}";
        }

        // Fallback: Use JSON serialization for complex queries
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var hash = ComputeHash(json);

        return $"query:{queryName.ToLowerInvariant()}:{hash}";
    }

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "empty";

        // Normalize: lowercase, trim, remove extra spaces, limit length
        var normalized = string.Join("_",
            query.ToLowerInvariant()
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(5)); // Limit to 5 words

        // Replace special characters
        normalized = new string(normalized
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray());

        return normalized.Length > 50 ? normalized[..50] : normalized;
    }

    private static string ComputeHash(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static bool IsResultType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(Result<>) || genericType == typeof(PagedResult<>);
    }
}
