using System.Diagnostics;
using OpenTelemetry.Trace;

namespace CineSocial.Api.Telemetry;

/// <summary>
/// Central registry for all ActivitySources used in the application for distributed tracing.
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// Service name used across all telemetry
    /// </summary>
    public const string ServiceName = "CineSocial.Api";

    /// <summary>
    /// Service version
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// ActivitySource for API layer operations
    /// </summary>
    public static readonly ActivitySource ApiSource = new(ServiceName, ServiceVersion);

    /// <summary>
    /// ActivitySource for Application layer operations (MediatR handlers, business logic)
    /// </summary>
    public static readonly ActivitySource ApplicationSource = new("CineSocial.Application", ServiceVersion);

    /// <summary>
    /// ActivitySource for Infrastructure layer operations (repositories, external services)
    /// </summary>
    public static readonly ActivitySource InfrastructureSource = new("CineSocial.Infrastructure", ServiceVersion);

    /// <summary>
    /// Creates a new activity (span) for API operations
    /// </summary>
    /// <param name="name">The operation name</param>
    /// <param name="kind">The activity kind (default: Internal)</param>
    /// <returns>New Activity or null if not sampled</returns>
    public static Activity? StartApiActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ApiSource.StartActivity(name, kind);
    }

    /// <summary>
    /// Creates a new activity (span) for Application layer operations
    /// </summary>
    /// <param name="name">The operation name</param>
    /// <param name="kind">The activity kind (default: Internal)</param>
    /// <returns>New Activity or null if not sampled</returns>
    public static Activity? StartApplicationActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ApplicationSource.StartActivity(name, kind);
    }

    /// <summary>
    /// Creates a new activity (span) for Infrastructure operations
    /// </summary>
    /// <param name="name">The operation name</param>
    /// <param name="kind">The activity kind (default: Client)</param>
    /// <returns>New Activity or null if not sampled</returns>
    public static Activity? StartInfrastructureActivity(string name, ActivityKind kind = ActivityKind.Client)
    {
        return InfrastructureSource.StartActivity(name, kind);
    }

    /// <summary>
    /// Adds a tag to the current activity if one exists
    /// </summary>
    public static void AddTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    /// <summary>
    /// Adds multiple tags to the current activity if one exists
    /// </summary>
    public static void AddTags(params (string Key, object? Value)[] tags)
    {
        var current = Activity.Current;
        if (current == null) return;

        foreach (var (key, value) in tags)
        {
            current.SetTag(key, value);
        }
    }

    /// <summary>
    /// Records an exception in the current activity
    /// </summary>
    public static void RecordException(Exception exception)
    {
        var current = Activity.Current;
        if (current == null) return;

        current.SetStatus(ActivityStatusCode.Error, exception.Message);
        current.RecordException(exception);
    }

    /// <summary>
    /// Sets the status of the current activity
    /// </summary>
    public static void SetStatus(ActivityStatusCode code, string? description = null)
    {
        Activity.Current?.SetStatus(code, description);
    }
}
