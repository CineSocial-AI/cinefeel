namespace CineSocial.Application.Common.Results;

/// <summary>
/// Represents a validation error with field-specific errors
/// </summary>
public sealed record ValidationError
{
    private ValidationError(Error[] errors)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Error> errors) =>
        new(errors.ToArray());

    public static ValidationError FromDictionary(IDictionary<string, string[]> errors)
    {
        var errorList = errors
            .SelectMany(kvp => kvp.Value.Select(error =>
                Error.Validation($"Validation.{kvp.Key}", error)))
            .ToArray();

        return new ValidationError(errorList);
    }

    public IDictionary<string, string[]> ToDictionary()
    {
        return Errors
            .GroupBy(e => e.Code.Replace("Validation.", string.Empty))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());
    }
}
