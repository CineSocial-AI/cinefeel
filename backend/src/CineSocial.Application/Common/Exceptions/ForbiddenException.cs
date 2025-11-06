using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when user lacks permission to access a resource
/// </summary>
public sealed class ForbiddenException : BaseException
{
    public ForbiddenException(string code, string message)
        : base(Error.Forbidden(code, message))
    {
    }

    public ForbiddenException()
        : base(Error.Forbidden("Auth.Forbidden", "You do not have permission to access this resource"))
    {
    }

    public ForbiddenException(string message)
        : base(Error.Forbidden("Auth.Forbidden", message))
    {
    }
}
