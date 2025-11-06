using System.Diagnostics;
using CineSocial.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService? _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService?.UserId?.ToString() ?? "Anonymous";
        var userName = _currentUserService?.Username ?? "Anonymous";

        _logger.LogInformation("Handling {RequestName} for User {UserId} ({UserName})",
            requestName, userId, userName);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
