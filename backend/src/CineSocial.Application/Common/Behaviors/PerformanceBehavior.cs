using System.Diagnostics;
using CineSocial.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService? _currentUserService;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, ICurrentUserService? currentUserService = null)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService?.UserId?.ToString() ?? "Anonymous";
            var userName = _currentUserService?.Username ?? "Anonymous";

            _logger.LogWarning("Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) User: {UserId} ({UserName})",
                requestName, elapsedMilliseconds, userId, userName);
        }

        return response;
    }
}
