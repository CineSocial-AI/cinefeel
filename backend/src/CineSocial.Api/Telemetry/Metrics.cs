using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace CineSocial.Api.Telemetry;

/// <summary>
/// Central registry for all custom metrics in the application.
/// Follows OpenTelemetry semantic conventions.
/// </summary>
public static class Metrics
{
    private const string MeterName = "CineSocial.Api";
    private const string MeterVersion = "1.0.0";

    /// <summary>
    /// Meter instance for creating instruments
    /// </summary>
    public static readonly Meter AppMeter = new(MeterName, MeterVersion);

    // ========== HTTP Metrics ==========

    /// <summary>
    /// Counter for total HTTP requests received
    /// </summary>
    public static readonly Counter<long> HttpRequestCount = AppMeter.CreateCounter<long>(
        "http.server.requests.total",
        unit: "{request}",
        description: "Total number of HTTP requests received");

    /// <summary>
    /// Histogram for HTTP request duration
    /// </summary>
    public static readonly Histogram<double> HttpRequestDuration = AppMeter.CreateHistogram<double>(
        "http.server.request.duration",
        unit: "ms",
        description: "HTTP request duration in milliseconds");

    /// <summary>
    /// Counter for HTTP errors
    /// </summary>
    public static readonly Counter<long> HttpErrorCount = AppMeter.CreateCounter<long>(
        "http.server.errors.total",
        unit: "{error}",
        description: "Total number of HTTP errors");

    // ========== Database Metrics ==========

    /// <summary>
    /// Counter for database queries executed
    /// </summary>
    public static readonly Counter<long> DatabaseQueryCount = AppMeter.CreateCounter<long>(
        "db.queries.total",
        unit: "{query}",
        description: "Total number of database queries executed");

    /// <summary>
    /// Histogram for database query duration
    /// </summary>
    public static readonly Histogram<double> DatabaseQueryDuration = AppMeter.CreateHistogram<double>(
        "db.query.duration",
        unit: "ms",
        description: "Database query execution time in milliseconds");

    /// <summary>
    /// Counter for database errors
    /// </summary>
    public static readonly Counter<long> DatabaseErrorCount = AppMeter.CreateCounter<long>(
        "db.errors.total",
        unit: "{error}",
        description: "Total number of database errors");

    // ========== Business Metrics ==========

    /// <summary>
    /// Counter for user registrations
    /// </summary>
    public static readonly Counter<long> UserRegistrationCount = AppMeter.CreateCounter<long>(
        "app.users.registered.total",
        unit: "{user}",
        description: "Total number of user registrations");

    /// <summary>
    /// Counter for user logins
    /// </summary>
    public static readonly Counter<long> UserLoginCount = AppMeter.CreateCounter<long>(
        "app.users.login.total",
        unit: "{login}",
        description: "Total number of successful user logins");

    /// <summary>
    /// Counter for failed login attempts
    /// </summary>
    public static readonly Counter<long> UserLoginFailureCount = AppMeter.CreateCounter<long>(
        "app.users.login.failures.total",
        unit: "{failure}",
        description: "Total number of failed login attempts");

    /// <summary>
    /// Gauge for active users (currently logged in)
    /// </summary>
    public static readonly ObservableGauge<int> ActiveUsersCount = AppMeter.CreateObservableGauge<int>(
        "app.users.active",
        () => GetActiveUsersCount(),
        unit: "{user}",
        description: "Number of currently active users");

    /// <summary>
    /// Counter for movies added to favorites
    /// </summary>
    public static readonly Counter<long> MovieFavoriteCount = AppMeter.CreateCounter<long>(
        "app.movies.favorites.total",
        unit: "{favorite}",
        description: "Total number of movies added to favorites");

    /// <summary>
    /// Counter for movie ratings
    /// </summary>
    public static readonly Counter<long> MovieRatingCount = AppMeter.CreateCounter<long>(
        "app.movies.ratings.total",
        unit: "{rating}",
        description: "Total number of movie ratings");

    /// <summary>
    /// Counter for comments created
    /// </summary>
    public static readonly Counter<long> CommentCount = AppMeter.CreateCounter<long>(
        "app.comments.created.total",
        unit: "{comment}",
        description: "Total number of comments created");

    /// <summary>
    /// Counter for list creations
    /// </summary>
    public static readonly Counter<long> ListCreationCount = AppMeter.CreateCounter<long>(
        "app.lists.created.total",
        unit: "{list}",
        description: "Total number of lists created");

    // ========== MediatR Metrics ==========

    /// <summary>
    /// Counter for MediatR requests
    /// </summary>
    public static readonly Counter<long> MediatRRequestCount = AppMeter.CreateCounter<long>(
        "app.mediatr.requests.total",
        unit: "{request}",
        description: "Total number of MediatR requests processed");

    /// <summary>
    /// Histogram for MediatR request duration
    /// </summary>
    public static readonly Histogram<double> MediatRRequestDuration = AppMeter.CreateHistogram<double>(
        "app.mediatr.request.duration",
        unit: "ms",
        description: "MediatR request processing time in milliseconds");

    /// <summary>
    /// Counter for MediatR failures
    /// </summary>
    public static readonly Counter<long> MediatRFailureCount = AppMeter.CreateCounter<long>(
        "app.mediatr.failures.total",
        unit: "{failure}",
        description: "Total number of MediatR request failures");

    // ========== External API Metrics ==========

    /// <summary>
    /// Counter for external API calls (e.g., TMDb)
    /// </summary>
    public static readonly Counter<long> ExternalApiCallCount = AppMeter.CreateCounter<long>(
        "app.external.api.calls.total",
        unit: "{call}",
        description: "Total number of external API calls");

    /// <summary>
    /// Histogram for external API call duration
    /// </summary>
    public static readonly Histogram<double> ExternalApiCallDuration = AppMeter.CreateHistogram<double>(
        "app.external.api.call.duration",
        unit: "ms",
        description: "External API call duration in milliseconds");

    /// <summary>
    /// Counter for external API errors
    /// </summary>
    public static readonly Counter<long> ExternalApiErrorCount = AppMeter.CreateCounter<long>(
        "app.external.api.errors.total",
        unit: "{error}",
        description: "Total number of external API errors");

    // ========== Cache Metrics ==========

    /// <summary>
    /// Counter for cache hits
    /// </summary>
    public static readonly Counter<long> CacheHitCount = AppMeter.CreateCounter<long>(
        "app.cache.hits.total",
        unit: "{hit}",
        description: "Total number of cache hits");

    /// <summary>
    /// Counter for cache misses
    /// </summary>
    public static readonly Counter<long> CacheMissCount = AppMeter.CreateCounter<long>(
        "app.cache.misses.total",
        unit: "{miss}",
        description: "Total number of cache misses");

    // ========== Helper Methods ==========

    private static int GetActiveUsersCount()
    {
        // This will be implemented with actual logic later
        // For now, return 0 as a placeholder
        return 0;
    }

    /// <summary>
    /// Records an HTTP request with common tags
    /// </summary>
    public static void RecordHttpRequest(string method, string route, int statusCode, double durationMs)
    {
        var tags = new TagList
        {
            { "http.method", method },
            { "http.route", route },
            { "http.status_code", statusCode }
        };

        HttpRequestCount.Add(1, tags);
        HttpRequestDuration.Record(durationMs, tags);

        if (statusCode >= 400)
        {
            HttpErrorCount.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a database query
    /// </summary>
    public static void RecordDatabaseQuery(string operation, double durationMs, bool success = true)
    {
        var tags = new TagList
        {
            { "db.operation", operation },
            { "db.success", success }
        };

        DatabaseQueryCount.Add(1, tags);
        DatabaseQueryDuration.Record(durationMs, tags);

        if (!success)
        {
            DatabaseErrorCount.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a MediatR request
    /// </summary>
    public static void RecordMediatRRequest(string requestName, double durationMs, bool success = true)
    {
        var tags = new TagList
        {
            { "request.name", requestName },
            { "request.success", success }
        };

        MediatRRequestCount.Add(1, tags);
        MediatRRequestDuration.Record(durationMs, tags);

        if (!success)
        {
            MediatRFailureCount.Add(1, tags);
        }
    }
}
