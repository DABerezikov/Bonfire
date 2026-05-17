using System.Diagnostics.CodeAnalysis;

namespace BonfireDB.Entities.Base;

/// <summary>
/// Simple Result pattern implementation to model success/failure without exceptions.
/// </summary>
/// <typeparam name="T">Type contained in a successful result.</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && error is not null)
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("A failed result must contain an error message.", nameof(error));

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Indicates whether the result represents a successful operation.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the result represents a failed operation.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Value available when <see cref="IsSuccess"/> is true.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Error message available when <see cref="IsFailure"/> is true.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Failure(string error) => new(false, default, error);

    /// <summary>
    /// Returns the value if successful, otherwise throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public T ValueOrThrow()
    {
        if (IsFailure)
            throw new InvalidOperationException(Error);

        return Value!;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the provided fallback.
    /// </summary>
    public T ValueOrDefault(T fallback) => IsSuccess ? Value! : fallback;

    /// <summary>
    /// Allows piping another operation that returns <see cref="Result{T}"/>.
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        if (binder is null)
            throw new ArgumentNullException(nameof(binder));

        return IsFailure ? Result<TResult>.Failure(Error!) : binder(Value!);
    }

    /// <summary>
    /// Allows pattern matching on the outcome.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        if (onSuccess is null)
            throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null)
            throw new ArgumentNullException(nameof(onFailure));

        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}

/// <summary>
/// Convenience helpers for creating results without specifying the generic parameter explicitly.
/// </summary>
public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

    public static Result<T> From<T>([MaybeNull] T value, string? error = null)
        => error is null ? Success(value!) : Failure<T>(error);
}

