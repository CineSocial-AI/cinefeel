using CineSocial.Application.Common.Interfaces;
using CineSocial.Infrastructure.Data;
using CineSocial.Infrastructure.Repositories;
using CineSocial.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CineSocial.Api.Tests;

public class GraphQLTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing
        builder.UseEnvironment("Testing");

        // Override configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SECRET"] = "test-secret-key-with-minimum-256-bits-for-testing-purposes-only",
                ["JWT_ISSUER"] = "TestIssuer",
                ["JWT_AUDIENCE"] = "TestAudience",
                // Set dummy database values to prevent connection string errors
                ["DATABASE_HOST"] = "localhost",
                ["DATABASE_PORT"] = "5432",
                ["DATABASE_NAME"] = "test",
                ["DATABASE_USER"] = "test",
                ["DATABASE_PASSWORD"] = "test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing ApplicationDbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove IApplicationDbContext
            var appDbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IApplicationDbContext));
            if (appDbContextDescriptor != null)
            {
                services.Remove(appDbContextDescriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Re-add IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            // Ensure other infrastructure services are still available
            services.TryAddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.TryAddScoped<IUnitOfWork, UnitOfWork>();
            services.TryAddScoped<IJwtService, JwtService>();

            // Build the service provider and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            // Seed test data
            SeedTestData(db);
        });
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Add test genres
        if (!context.Genres.Any())
        {
            context.Genres.AddRange(
                new Domain.Entities.Movie.Genre { Id = 1, TmdbId = 28, Name = "Action" },
                new Domain.Entities.Movie.Genre { Id = 2, TmdbId = 35, Name = "Comedy" },
                new Domain.Entities.Movie.Genre { Id = 3, TmdbId = 18, Name = "Drama" }
            );
        }

        // Add test user
        if (!context.Users.Any(u => u.Username == "testuser"))
        {
            context.Users.Add(new Domain.Entities.User.AppUser
            {
                Id = 100,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = Domain.Enums.UserRole.User,
                IsActive = true
            });
        }

        // Add test movie
        if (!context.Movies.Any())
        {
            context.Movies.Add(new Domain.Entities.Movie.MovieEntity
            {
                Id = 1,
                TmdbId = 550,
                Title = "Fight Club",
                OriginalTitle = "Fight Club",
                Overview = "A ticking-time-bomb insomniac and a slippery soap salesman channel primal male aggression into a shocking new form of therapy.",
                ReleaseDate = new DateTime(1999, 10, 15),
                Runtime = 139,
                Budget = 63000000,
                Revenue = 100853753,
                VoteAverage = 8.4,
                VoteCount = 26280,
                Popularity = 61.416,
                Status = "Released",
                Adult = false
            });
        }

        context.SaveChanges();
    }
}
