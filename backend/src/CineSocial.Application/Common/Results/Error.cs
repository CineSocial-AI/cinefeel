namespace CineSocial.Application.Common.Results;

/// <summary>
/// Represents an error in the application
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided", ErrorType.Validation);

    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);

    public int GetHttpStatusCode() => Type switch
    {
        ErrorType.NotFound => 404,
        ErrorType.Validation => 400,
        ErrorType.Conflict => 409,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.Unexpected => 500,
        ErrorType.Failure => 500,
        _ => 500
    };
}

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Unexpected = 7
}
