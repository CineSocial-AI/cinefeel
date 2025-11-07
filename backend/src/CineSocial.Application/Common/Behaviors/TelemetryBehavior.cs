using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace CineSocial.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for telemetry (tracing and metrics).
/// Automatically creates spans and records metrics for all requests.
/// </summary>
public class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string ActivitySourceName = "CineSocial.Application";
    private const string MeterName = "CineSocial.Application";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    private static readonly System.Diagnostics.Metrics.Meter Meter = new(MeterName, "1.0.0");

    // Metrics
    private static readonly System.Diagnostics.Metrics.Counter<long> RequestCounter =
        Meter.CreateCounter<long>("mediatr.requests.total", "{request}", "Total number of MediatR requests");

    private static readonly System.Diagnostics.Metrics.Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("mediatr.request.duration", "ms", "MediatR request duration");

    private static readonly System.Diagnostics.Metrics.Counter<long> RequestFailureCounter =
        Meter.CreateCounter<long>("mediatr.requests.failures.total", "{failure}", "Total number of failed MediatR requests");

    private readonly ILogger<TelemetryBehavior<TRequest, TResponse>> _logger;

    public TelemetryBehavior(ILogger<TelemetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Start a new activity (span) for this request
        using var activity = ActivitySource.StartActivity($"MediatR.{requestName}", ActivityKind.Internal);

        activity?.SetTag("mediatr.request.type", typeof(TRequest).FullName);
        activity?.SetTag("mediatr.response.type", typeof(TResponse).FullName);

        var stopwatch = Stopwatch.StartNew();
        var success = false;
        Exception? exception = null;

        try
        {
            _logger.LogDebug("Executing MediatR request: {RequestName}", requestName);

            var response = await next();

            success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogDebug("MediatR request completed successfully: {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);

            _logger.LogError(ex, "MediatR request failed: {RequestName}", requestName);

            throw;
        }
        finally
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            activity?.SetTag("mediatr.request.duration_ms", durationMs);
            activity?.SetTag("mediatr.request.success", success);

            // Record metrics
            var tags = new TagList
            {
                { "request.name", requestName },
                { "request.success", success }
            };

            if (exception != null)
            {
                tags.Add("exception.type", exception.GetType().Name);
            }

            RequestCounter.Add(1, tags);
            RequestDuration.Record(durationMs, tags);

            if (!success)
            {
                RequestFailureCounter.Add(1, tags);
            }

            _logger.LogInformation(
                "MediatR request {RequestName} completed in {DurationMs}ms with status: {Success}",
                requestName,
                durationMs,
                success ? "Success" : "Failed");
        }
    }
}
