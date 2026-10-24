using System.Diagnostics;

namespace Bonfire.Domain.Functional;

public static class OptionExtensions
{
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
