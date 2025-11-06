using System.Text;
using CineSocial.Api.Middleware;
using CineSocial.Application.DependencyInjection;
using CineSocial.Infrastructure.DependencyInjection;
using DotNetEnv;
using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load .env file from infrastructure folder
// Try multiple paths to find infrastructure/.env file
var possiblePaths = new[]
{
    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "infrastructure", ".env"),  // From bin/Debug/net9.0
    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "infrastructure", ".env"),        // From src/CineSocial.Api
    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "infrastructure", ".env"),                          // From backend root
    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "..", "infrastructure", ".env"),                    // From backend/src
    System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "infrastructure", ".env")    // From published app
};

foreach (var envPath in possiblePaths)
{
    var fullPath = System.IO.Path.GetFullPath(envPath);
    if (File.Exists(fullPath))
    {
        Env.Load(fullPath);
        Log.Information("Loaded .env file from: {Path}", fullPath);
        break;
    }
}

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add Application & Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure OpenTelemetry with Jaeger
var jaegerEndpoint = builder.Configuration["JAEGER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
Log.Information("Jaeger OTLP Endpoint: {JaegerEndpoint}", jaegerEndpoint);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["ELASTIC_APM_SERVICE_NAME"] ?? "CineSocial.Api",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) => !httpContext.Request.Path.Value?.Contains("/swagger") ?? true;
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(jaegerEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        })
        .AddConsoleExporter());

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
        Description = "CineSocial REST API"
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
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CineSocial";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CineSocialUsers";

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

// Add Elastic APM middleware (should be early in the pipeline)
app.UseAllElasticApm(builder.Configuration);

// Add Serilog request logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

try
{
    Log.Information("Starting CineSocial API");
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

// Make the implicit Program class public so test projects can access it
public partial class Program { }
