using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public sealed class ValidationException : BaseException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(Error.Validation("Validation.Failed", "One or more validation errors occurred"))
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base(Error.Validation($"Validation.{field}", message))
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }

    public ValidationException(string code, string message, IDictionary<string, string[]> errors)
        : base(Error.Validation(code, message))
    {
        Errors = errors;
    }
}
