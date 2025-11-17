using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CineSocial.Infrastructure.Caching;

/// <summary>
/// Generates cache keys for search queries with normalization
/// </summary>
public static class SearchCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for search queries
    /// Normalizes query string (lowercase, trim) and includes pagination
    /// </summary>
    public static string GenerateKey(string queryType, string searchQuery, int page, int pageSize)
    {
        // Normalize search query (lowercase, trim, remove extra spaces)
        var normalizedQuery = NormalizeSearchQuery(searchQuery);

        // Create a deterministic key
        var keyData = new
        {
            Type = queryType.ToLowerInvariant(),
            Query = normalizedQuery,
            Page = page,
            PageSize = pageSize
        };

        var json = JsonSerializer.Serialize(keyData);
        var hash = GenerateHash(json);

        return $"search:{queryType.ToLowerInvariant()}:{hash}";
    }

    /// <summary>
    /// Generates a cache key for complex search queries with filters
    /// </summary>
    public static string GenerateKeyWithFilters<TQuery>(TQuery query) where TQuery : class
    {
        var json = JsonSerializer.Serialize(query, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var hash = GenerateHash(json);
        var queryType = typeof(TQuery).Name.Replace("Query", "").ToLowerInvariant();

        return $"search:{queryType}:{hash}";
    }

    /// <summary>
    /// Normalizes search query for consistent cache keys
    /// </summary>
    private static string NormalizeSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Lowercase, trim, and remove extra spaces
        return string.Join(" ",
            query.ToLowerInvariant()
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Generates a short hash for cache key
    /// </summary>
    private static string GenerateHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant()[..16]; // Use first 16 chars
    }

    /// <summary>
    /// Gets cache key prefix for all searches of a specific type
    /// </summary>
    public static string GetSearchPrefix(string searchType)
    {
        return $"search:{searchType.ToLowerInvariant()}:";
    }

    /// <summary>
    /// Gets cache key prefix for all search results
    /// </summary>
    public static string GetAllSearchPrefix()
    {
        return "search:";
    }
}
