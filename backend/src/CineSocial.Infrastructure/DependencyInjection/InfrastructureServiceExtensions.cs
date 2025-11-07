using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Services;
using CineSocial.Infrastructure.Data;
using CineSocial.Infrastructure.Repositories;
using CineSocial.Infrastructure.Security;
using CineSocial.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Repository & UnitOfWork
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Image Service
        services.AddScoped<IImageService, ImageService>();

        return services;
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
