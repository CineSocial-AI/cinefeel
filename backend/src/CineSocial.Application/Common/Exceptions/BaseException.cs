using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Base exception for all custom application exceptions
/// </summary>
public abstract class BaseException : Exception
{
    public Error Error { get; }

    protected BaseException(Error error) : base(error.Description)
    {
        Error = error;
    }

    protected BaseException(Error error, Exception innerException)
        : base(error.Description, innerException)
    {
        Error = error;
    }
}
