using CineSocial.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CineSocial.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));

        // Add QueryCachingBehavior first (before telemetry) for cacheable queries
        // This allows cache hits to skip expensive operations and reduce telemetry overhead
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));

        // Add TelemetryBehavior for comprehensive observability (tracing + metrics)
        // This replaces the old LoggingBehavior and PerformanceBehavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TelemetryBehavior<,>));

        return services;
    }
}
