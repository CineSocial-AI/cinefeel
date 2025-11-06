using System.Net;
using System.Text.Json;
using CineSocial.Application.Common.Exceptions;
using CineSocial.Application.Common.Results;
using Error = CineSocial.Application.Common.Results.Error;

namespace CineSocial.Api.Middleware;

/// <summary>
/// Global exception handler middleware for catching and formatting exceptions
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        Error error;

        switch (exception)
        {
            case BaseException baseException:
                statusCode = baseException.Error.GetHttpStatusCode();
                error = baseException.Error;
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                error = Error.Unexpected("Error.Unexpected", "An unexpected error occurred");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        object response;

        if (exception is ValidationException validationException)
        {
            response = new
            {
                success = false,
                error = new
                {
                    code = error.Code,
                    description = error.Description,
                    type = error.Type.ToString()
                },
                errors = validationException.Errors
            };
        }
        else
        {
            response = new
            {
                success = false,
                error = new
                {
                    code = error.Code,
                    description = error.Description,
                    type = error.Type.ToString()
                }
            };
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Extension method for registering the global exception handler middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
