namespace CineSocial.Application.Common.Results;

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    // Railway-oriented programming
    public Result Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(Error);
        }
        return this;
    }

    // Implicit operator for easy conversion
    public static implicit operator Result(Error error) => Failure(error);
}
