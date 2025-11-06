using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs (e.g., duplicate entry)
/// </summary>
public sealed class ConflictException : BaseException
{
    public ConflictException(string code, string message)
        : base(Error.Conflict(code, message))
    {
    }

    public ConflictException(string entityName, string identifier, bool isStringIdentifier)
        : base(Error.Conflict($"{entityName}.AlreadyExists", $"{entityName} '{identifier}' already exists"))
    {
    }
}