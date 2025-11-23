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

        // Get connection string from DATABASE_URL or build from individual variables
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var connectionString = !string.IsNullOrEmpty(databaseUrl)
            ? ConvertPostgresUrlToConnectionString(databaseUrl)
            : Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
              ?? BuildConnectionStringFromEnv();

        Console.WriteLine($"[FACTORY] Using connection string");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
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

        // Parse query parameters
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

    private static string BuildConnectionStringFromEnv()
    {
        // Fallback to legacy individual environment variables
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST")
            ?? throw new InvalidOperationException("DATABASE_URL or DATABASE_HOST not found in environment variables");
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "CINE";
        var username = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "cinesocial";
        var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "cinesocial123";
        var sslMode = Environment.GetEnvironmentVariable("DATABASE_SSL_MODE") ?? "Disable";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
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
