using System.Diagnostics;
using CineSocial.Api.Telemetry;

namespace CineSocial.Api.Middleware;

/// <summary>
/// Middleware that ensures every HTTP request has a unique correlation ID.
/// Follows W3C Trace Context standard when available, otherwise generates a new ID.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string TraceParentHeaderName = "traceparent";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

        // Add to diagnostic context for logging
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            // Add to current activity (span) for tracing
            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                currentActivity.SetTag("correlation.id", correlationId);
                currentActivity.SetBaggage("correlation.id", correlationId);
            }

            // Add to HttpContext for easy access throughout the request pipeline
            context.Items["CorrelationId"] = correlationId;

            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // 1. Check if correlation ID is provided in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            _logger.LogDebug("Using provided correlation ID: {CorrelationId}", correlationId.ToString());
            return correlationId.ToString();
        }

        // 2. Try to extract from W3C Trace Context (traceparent header)
        if (context.Request.Headers.TryGetValue(TraceParentHeaderName, out var traceParent)
            && !string.IsNullOrWhiteSpace(traceParent))
        {
            var traceId = ExtractTraceIdFromTraceParent(traceParent.ToString());
            if (!string.IsNullOrWhiteSpace(traceId))
            {
                _logger.LogDebug("Extracted trace ID from traceparent: {TraceId}", traceId);
                return traceId;
            }
        }

        // 3. Use current Activity's TraceId if available
        var currentActivity = Activity.Current;
        if (currentActivity != null && currentActivity.TraceId != default)
        {
            var traceId = currentActivity.TraceId.ToString();
            _logger.LogDebug("Using Activity TraceId as correlation ID: {TraceId}", traceId);
            return traceId;
        }

        // 4. Generate a new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString("N");
        _logger.LogDebug("Generated new correlation ID: {CorrelationId}", newCorrelationId);
        return newCorrelationId;
    }

    private string? ExtractTraceIdFromTraceParent(string traceParent)
    {
        try
        {
            // W3C traceparent format: version-trace-id-parent-id-trace-flags
            // Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
            var parts = traceParent.Split('-');
            if (parts.Length >= 2)
            {
                return parts[1]; // trace-id is the second part
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract trace ID from traceparent: {TraceParent}", traceParent);
        }

        return null;
    }
}

/// <summary>
/// Extension methods for registering the CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationIdMiddleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

/// <summary>
/// Helper to access correlation ID from anywhere in the request pipeline
/// </summary>
public static class CorrelationIdAccessor
{
    /// <summary>
    /// Gets the correlation ID from the current HttpContext
    /// </summary>
    public static string? GetCorrelationId(HttpContext? context)
    {
        if (context == null) return null;

        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            return correlationId?.ToString();
        }

        // Fallback to Activity TraceId
        var currentActivity = Activity.Current;
        if (currentActivity != null && currentActivity.TraceId != default)
        {
            return currentActivity.TraceId.ToString();
        }

        return null;
    }

    /// <summary>
    /// Gets the correlation ID from the current Activity baggage
    /// </summary>
    public static string? GetCorrelationIdFromBaggage()
    {
        var currentActivity = Activity.Current;
        return currentActivity?.GetBaggageItem("correlation.id");
    }
}
