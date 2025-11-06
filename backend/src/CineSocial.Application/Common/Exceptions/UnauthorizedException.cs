using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when authentication is required but not provided or invalid
/// </summary>
public sealed class UnauthorizedException : BaseException
{
    public UnauthorizedException(string code, string message)
        : base(Error.Unauthorized(code, message))
    {
    }

    public UnauthorizedException()
        : base(Error.Unauthorized("Auth.Unauthorized", "You are not authenticated"))
    {
    }

    public UnauthorizedException(string message)
        : base(Error.Unauthorized("Auth.Unauthorized", message))
    {
    }
}
