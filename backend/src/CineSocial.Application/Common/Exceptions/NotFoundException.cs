using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public sealed class NotFoundException : BaseException
{
    public NotFoundException(string code, string message)
        : base(Error.NotFound(code, message))
    {
    }

    public NotFoundException(string entityName, Guid id)
        : base(Error.NotFound($"{entityName}.NotFound", $"{entityName} with ID {id} was not found"))
    {
    }

    public NotFoundException(string entityName, string identifier, bool isStringIdentifier)
        : base(Error.NotFound($"{entityName}.NotFound", $"{entityName} '{identifier}' was not found"))
    {
    }
}