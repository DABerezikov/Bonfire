using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Bonfire.Domain.Functional;

/// <summary>Явный результат: значение либо сообщение ожидаемого отказа.</summary>
public sealed class Result<T>
{
    private abstract record Outcome;
    private sealed record SuccessValue(T Item) : Outcome;
    private sealed record FailureValue(string Message) : Outcome;

    private readonly Outcome _outcome;

    private Result(Outcome outcome) => _outcome = outcome;

    public bool IsSuccess => _outcome is SuccessValue;
    public bool IsFailure => _outcome is FailureValue;

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(new SuccessValue(value));
    }

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Сообщение об ошибке не может быть пустым", nameof(error));
        return new(new FailureValue(error));
    }

    public T ValueOrThrow() => Match(value => value, error => throw new InvalidOperationException(error));

    public T ValueOrDefault(T fallback) => Match(value => value, _ => fallback);

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return Match(binder, Result<TResult>.Failure);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return _outcome switch
        {
            SuccessValue success => onSuccess(success.Item),
            FailureValue failure => onFailure(failure.Message),
            _ => throw new UnreachableException()
        };
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

    public static Result<T> From<T>([AllowNull] T value, string? error = null) => error is not null
        ? Failure<T>(error)
        : value is null
            ? Failure<T>("Значение отсутствует")
            : Success(value);
}
