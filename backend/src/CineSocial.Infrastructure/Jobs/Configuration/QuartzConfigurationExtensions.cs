using CineSocial.Infrastructure.Jobs.Email;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace CineSocial.Infrastructure.Jobs.Configuration;

public static class QuartzConfigurationExtensions
{
    public static void ConfigureQuartz(this IServiceCollectionQuartzConfigurator configurator, IConfiguration configuration)
    {
        // Scheduler configuration
        var schedulerName = configuration["QUARTZ_SCHEDULER_NAME"] ?? "CineSocialScheduler";
        configurator.SchedulerId = "AUTO";
        configurator.SchedulerName = schedulerName;

        // Use PostgreSQL for job storage
        var usePostgreSqlString = configuration["Quartz:UsePostgreSQL"];
        var usePostgreSql = string.IsNullOrEmpty(usePostgreSqlString) ? true : bool.Parse(usePostgreSqlString);

        if (usePostgreSql)
        {
            // Build connection string from env vars
            var host = configuration["DATABASE_HOST"] ?? "localhost";
            var port = configuration["DATABASE_PORT"] ?? "5432";
            var database = configuration["DATABASE_NAME"] ?? "CINE";
            var username = configuration["DATABASE_USER"] ?? "cinesocial";
            var password = configuration["DATABASE_PASSWORD"] ?? "cinesocial123";

            var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable;Trust Server Certificate=true";

            configurator.UsePersistentStore(store =>
            {
                store.UsePostgres(postgres =>
                {
                    postgres.ConnectionString = connectionString;
                    postgres.TablePrefix = "qrtz_";
                });

                store.UseJsonSerializer();
                store.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });
            });
        }
        else
        {
            // Use in-memory storage (for development)
            configurator.UseInMemoryStore();
        }

        // Thread pool configuration
        var threadPoolSizeString = configuration["QUARTZ_THREAD_POOL_SIZE"];
        var threadPoolSize = string.IsNullOrEmpty(threadPoolSizeString) ? 10 : int.Parse(threadPoolSizeString);
        configurator.UseDefaultThreadPool(tp =>
        {
            tp.MaxConcurrency = threadPoolSize;
        });

        // Job configuration
        configurator.AddJob<SendEmailVerificationJob>(j => j
            .WithIdentity(nameof(SendEmailVerificationJob), "Email")
            .StoreDurably(true)
        );

        // Additional Quartz options
        configurator.UseMicrosoftDependencyInjectionJobFactory();

        // Configure misfire behavior
        configurator.SchedulerId = schedulerName;
        configurator.MisfireThreshold = TimeSpan.FromSeconds(60);
    }
}
