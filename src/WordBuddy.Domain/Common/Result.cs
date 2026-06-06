using System.Diagnostics.CodeAnalysis;

namespace WordBuddy.Domain.Common;

/// <summary>Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the result value. Non-null when <see cref="IsSuccess"/> is <see langword="true"/>;
    /// <see langword="default"/> on failure — always guard with <see cref="IsSuccess"/> before reading.
    /// </summary>
    public T? Value { get; }

    /// <summary>Gets the error. <see cref="Common.Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(true, value, Error.None);

    /// <summary>Creates a failed result wrapping <paramref name="error"/>.</summary>
    public static Result<T> Failure(Error error) => new(false, default, error);
}

/// <summary>Represents the outcome of an operation that produces no value.</summary>
public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error. <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>Pre-allocated successful void result.</summary>
    public static readonly Result Success = new(true, Error.None);

    /// <summary>Creates a failed void result wrapping <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);
}
