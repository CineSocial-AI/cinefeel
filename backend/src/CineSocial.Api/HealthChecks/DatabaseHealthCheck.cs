using CineSocial.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CineSocial.Api.HealthChecks;

/// <summary>
/// Custom health check for database connectivity and responsiveness
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ApplicationDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to connect to database
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        ["database"] = "PostgreSQL",
                        ["response_time_ms"] = stopwatch.ElapsedMilliseconds
                    });
            }

            var responseTime = stopwatch.ElapsedMilliseconds;

            var data = new Dictionary<string, object>
            {
                ["database"] = "PostgreSQL",
                ["response_time_ms"] = responseTime
            };

            // Consider degraded if response time > 1 second
            if (responseTime > 1000)
            {
                _logger.LogWarning("Database health check slow: {ResponseTimeMs}ms", responseTime);
                return HealthCheckResult.Degraded(
                    $"Database responding slowly ({responseTime}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Database is healthy",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["database"] = "PostgreSQL",
                    ["error"] = ex.Message
                });
        }
    }
}
