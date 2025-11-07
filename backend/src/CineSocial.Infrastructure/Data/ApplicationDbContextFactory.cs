using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CineSocial.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Load .env file from infrastructure directory
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "infrastructure", ".env");
        if (File.Exists(envPath))
        {
            LoadEnvFile(envPath);
            Console.WriteLine($"[FACTORY] Loaded .env from: {envPath}");
        }
        else
        {
            Console.WriteLine($"[FACTORY] .env not found at: {envPath}");
        }

        // Build connection string from environment variables
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST")
            ?? throw new InvalidOperationException("DATABASE_HOST not found in environment variables");
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT")
            ?? throw new InvalidOperationException("DATABASE_PORT not found in environment variables");
        var database = Environment.GetEnvironmentVariable("DATABASE_NAME")
            ?? throw new InvalidOperationException("DATABASE_NAME not found in environment variables");
        var username = Environment.GetEnvironmentVariable("DATABASE_USER")
            ?? throw new InvalidOperationException("DATABASE_USER not found in environment variables");
        var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD")
            ?? throw new InvalidOperationException("DATABASE_PASSWORD not found in environment variables");
        var sslMode = Environment.GetEnvironmentVariable("DATABASE_SSL_MODE")
            ?? throw new InvalidOperationException("DATABASE_SSL_MODE not found in environment variables");

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";

        Console.WriteLine($"[FACTORY] Connection string: Host={host};Port={port};Database={database};Username={username}");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static void LoadEnvFile(string filePath)
    {
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}
