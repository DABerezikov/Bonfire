using System.Diagnostics;

namespace Bonfire.Domain.Functional;

public static class OptionExtensions
{
    public static Option<T> ToOption<T>(this T? value) where T : struct
        => value is { } present ? Option.Some(present) : Option.None<T>();

    public static Option<string> ToOption(this string? value)
        => value is { } present ? Option.Some(present) : Option.None<string>();

    public static T? ToNullable<T>(this Option<T> source) where T : struct
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Match<T?>(present => present, () => null);
    }

    public static string? ToNullable(this Option<string> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Match<string?>(present => present, () => null);
    }

    public static Option<TResult> Map<T, TResult>(this Option<T> source, Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mapper);
        return source switch
        {
            Option<T>.Some some => Option.Some(mapper(some.Value)),
            Option<T>.None => Option.None<TResult>(),
            _ => throw new UnreachableException()
        };
    }
}