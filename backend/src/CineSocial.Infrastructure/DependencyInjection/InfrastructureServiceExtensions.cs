using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Services;
using CineSocial.Infrastructure.Caching;
using CineSocial.Infrastructure.Data;
using CineSocial.Infrastructure.Email;
using CineSocial.Infrastructure.Repositories;
using CineSocial.Infrastructure.Security;
using CineSocial.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace CineSocial.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? configuration["DATABASE_CONNECTION_STRING"]
            ?? BuildConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Caching (Redis + FusionCache)
        AddCachingServices(services, configuration);

        // Repository & UnitOfWork
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Image Service
        services.AddScoped<IImageService, ImageService>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    private static void AddCachingServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get Redis connection string from environment or config
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
            ?? configuration["REDIS_CONNECTION_STRING"]
            ?? "localhost:6379";

        // Register Redis connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectTimeout = 5000;
            config.SyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(config);
        });

        // Register distributed cache (Redis L2 cache)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CineSocial:";
        });

        // Register memory cache (L1 cache)
        services.AddMemoryCache();

        // Register FusionCache with both L1 (memory) and L2 (Redis)
        services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                // Default cache duration
                options.Duration = TimeSpan.FromMinutes(10);

                // Enable fail-safe mechanism
                options.IsFailSafeEnabled = true;
                options.FailSafeMaxDuration = TimeSpan.FromHours(2);
                options.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);

                // Enable distributed cache (L2 - Redis)
                options.AllowBackgroundDistributedCacheOperations = true;
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDistributedCache();

        // Register custom cache service
        services.AddScoped<ICacheService, CacheService>();
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var host = configuration["DATABASE_HOST"];
        var port = configuration["DATABASE_PORT"];
        var database = configuration["DATABASE_NAME"];
        var username = configuration["DATABASE_USER"];
        var password = configuration["DATABASE_PASSWORD"];
        var sslMode = configuration["DATABASE_SSL_MODE"] ?? "Require";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }
}
