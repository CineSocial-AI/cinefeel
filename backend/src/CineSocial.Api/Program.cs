using System.Text;
using CineSocial.Api.HealthChecks;
using CineSocial.Api.Middleware;
using CineSocial.Api.Telemetry;
using CineSocial.Application.DependencyInjection;
using CineSocial.Infrastructure.DependencyInjection;
using CineSocial.Infrastructure.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
var envPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "infrastructure", ".env");
if (File.Exists(envPath))
{
    LoadEnvFile(envPath);
    Console.WriteLine($"[ENV] Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine($"[ENV] .env not found at: {envPath}");
}

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog with OpenTelemetry integration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithSpan() // Correlate logs with traces
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j} {TraceId} {SpanId}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/cinesocial-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j} {TraceId} {SpanId}{NewLine}{Exception}")
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
            options.Protocol = OtlpProtocol.Grpc;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = ActivitySources.ServiceName,
                ["service.version"] = ActivitySources.ServiceVersion
            };
        });
});

// Add Application & Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure OpenTelemetry with comprehensive observability
var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
Log.Information("OpenTelemetry OTLP Endpoint: {OtelEndpoint}", otelEndpoint);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: ActivitySources.ServiceName,
            serviceVersion: ActivitySources.ServiceVersion,
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    // Configure Tracing
    .WithTracing(tracing => tracing
        .AddSource(ActivitySources.ServiceName)
        .AddSource("CineSocial.Application")
        .AddSource("CineSocial.Infrastructure")
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) => !httpContext.Request.Path.Value?.Contains("/swagger") ?? true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
            };
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response_content_length", response.ContentLength);
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method.ToString());
            };
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                activity.SetTag("db.command_timeout", command.CommandTimeout);
            };
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otelEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    // Configure Metrics
    .WithMetrics(metrics => metrics
        .AddMeter(Metrics.AppMeter.Name)
        .AddMeter("CineSocial.Application")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otelEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        }));

// Configure Health Checks
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? BuildConnectionString();

var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? builder.Configuration["REDIS_CONNECTION_STRING"]
    ?? "localhost:6379";

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "ready" })
    .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "db", "ready" })
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache", "ready" });

// Add Health Checks UI
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
    options.AddHealthCheckEndpoint("CineSocial API", "/health");
})
.AddInMemoryStorage();

// Add Controllers for REST API
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CineSocial API",
        Version = "v1",
        Description = "CineSocial REST API with comprehensive observability"
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Support for file uploads
    options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineSocial";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineSocialUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CineSocial API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "CineSocial API";
    });
}

// app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add Correlation ID middleware (must be early in pipeline)
app.UseCorrelationId();

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Add Serilog request logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("CorrelationId", CorrelationIdAccessor.GetCorrelationId(httpContext));
    };
});

app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = _ => true
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks, just liveness
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = check => check.Tags.Contains("ready")
});

// Health Checks UI
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

try
{
    Log.Information("Starting CineSocial API with OpenTelemetry observability");
    Log.Information("Service Name: {ServiceName}, Version: {ServiceVersion}",
        ActivitySources.ServiceName, ActivitySources.ServiceVersion);
    Log.Information("OTLP Endpoint: {OtelEndpoint}", otelEndpoint);
    Log.Information("Prometheus Metrics: /metrics");
    Log.Information("Health Checks: /health, /health/live, /health/ready, /health-ui");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Helper methods
static void LoadEnvFile(string filePath)
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

static string BuildConnectionString()
{
    // Try DATABASE_URL first (PostgreSQL connection string format)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        return ConvertPostgresUrlToConnectionString(databaseUrl);
    }

    // Fall back to individual environment variables
    var host = Environment.GetEnvironmentVariable("DATABASE_HOST")
        ?? throw new InvalidOperationException("DATABASE_URL or DATABASE_HOST not configured");

    var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "CINE";
    var username = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "cinesocial";
    var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "cinesocial123";
    var sslMode = Environment.GetEnvironmentVariable("DATABASE_SSL_MODE") ?? "Disable";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
}

static string ConvertPostgresUrlToConnectionString(string databaseUrl)
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

// Make the implicit Program class public so test projects can access it
public partial class Program { }
