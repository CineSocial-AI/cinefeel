namespace CineSocial.Application.Common.Results;

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result");

    // Railway-oriented programming
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(Error);
    }

    public Result<TValue> Tap(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(_value!);
        }
        return this;
    }

    public Result<TResult> Map<TResult>(Func<TValue, TResult> mapper)
    {
        return IsSuccess
            ? Success(mapper(_value!))
            : Failure<TResult>(Error);
    }

    public Result<TResult> Bind<TResult>(Func<TValue, Result<TResult>> binder)
    {
        return IsSuccess
            ? binder(_value!)
            : Failure<TResult>(Error);
    }

    // Implicit operators for easy conversion
    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
