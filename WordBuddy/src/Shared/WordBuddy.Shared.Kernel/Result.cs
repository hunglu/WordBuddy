namespace WordBuddy.Shared.Kernel;

/// <summary>The outcome of an operation that can fail with a typed <see cref="Error"/>, without throwing.</summary>
public class Result
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error describing why the operation failed. <see cref="Error.None"/> when <see cref="IsSuccess"/> is <see langword="true"/>.</summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result carrying the given <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>Creates a failed result of type <typeparamref name="TValue"/> carrying the given <paramref name="error"/>.</summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>The outcome of an operation that produces a <typeparamref name="TValue"/> on success, or a typed <see cref="Error"/> on failure.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Gets the success value. Throws if accessed on a failed result — check <see cref="Result.IsSuccess"/> first.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Implicitly wraps a value as a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
