using System.Diagnostics;

namespace Bonfire.Domain.Functional;

public abstract record Option<T>
{
    private Option() { }

    public sealed record Some : Option<T>
    {
        public T Value { get; }
        internal Some(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed record None : Option<T>
    {
        internal None() { }
    }

    public TResult Match<TResult>(Func<T, TResult> present, Func<TResult> absent)
    {
        ArgumentNullException.ThrowIfNull(present);
        ArgumentNullException.ThrowIfNull(absent);
        return this switch
        {
            Some some => present(some.Value),
            None => absent(),
            _ => throw new UnreachableException()
        };
    }
}

public static class Option
{
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);
    public static Option<T> None<T>() => new Option<T>.None();
}
