using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Services;
using CineSocial.Infrastructure.Caching;
using CineSocial.Infrastructure.CloudStorage;
using CineSocial.Infrastructure.Data;
using CineSocial.Infrastructure.Email;
using CineSocial.Infrastructure.Jobs.Configuration;
using CineSocial.Infrastructure.Jobs.Services;
using CineSocial.Infrastructure.Repositories;
using CineSocial.Infrastructure.Security;
using CineSocial.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace CineSocial.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - Try DATABASE_URL first (Neon), then fall back to individual environment variables
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var connectionString = !string.IsNullOrEmpty(databaseUrl)
            ? ConvertPostgresUrlToConnectionString(databaseUrl)
            : Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
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

        // Cloud Storage (Cloudinary)
        services.Configure<CloudinarySettings>(options =>
        {
            options.CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
                ?? configuration["CLOUDINARY_CLOUD_NAME"]
                ?? string.Empty;
            options.ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
                ?? configuration["CLOUDINARY_API_KEY"]
                ?? string.Empty;
            options.ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
                ?? configuration["CLOUDINARY_API_SECRET"]
                ?? string.Empty;
            options.UploadFolder = Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_FOLDER")
                ?? configuration["CLOUDINARY_UPLOAD_FOLDER"]
                ?? "cinefeel";
        });
        services.AddScoped<ICloudStorageProvider, CloudinaryProvider>();

        // Image Service
        services.AddScoped<IImageService, ImageService>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Job Services
        services.AddScoped<IJobExecutionHistoryService, JobExecutionHistoryService>();
        services.AddScoped<IJobSchedulerService, JobSchedulerService>();

        // Quartz Job Scheduling
        services.AddQuartz(q => q.ConfigureQuartz(configuration));
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

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
            .TryWithAutoSetup()
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
            .WithSerializer(new FusionCacheSystemTextJsonSerializer());

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

    private static string ConvertPostgresUrlToConnectionString(string databaseUrl)
    {
        // Parse PostgreSQL URL format: postgresql://user:password@host:port/database?params
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        // Parse query parameters for SSL mode
        var sslMode = "Require";
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var sslModeParam = queryParams["sslmode"];
            if (!string.IsNullOrEmpty(sslModeParam))
            {
                sslMode = sslModeParam.ToLower() == "require" ? "Require" : "Prefer";
            }
        }

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }
}
