using System.Collections.Immutable;
using System.Diagnostics;

namespace Bonfire.Domain.Functional;

public static class ResultCollectionExtensions
{
    public static Result<ImmutableArray<TResult>> Traverse<T, TResult>(this IEnumerable<T> source,
        Func<T, Result<TResult>> map)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(map);
        var values = ImmutableArray.CreateBuilder<TResult>();
        foreach (var item in source)
        {
            var result = map(item);
            if (result.IsFailure)
                return result.Match(_ => throw new UnreachableException(), Result.Failure<ImmutableArray<TResult>>);
            values.Add(result.ValueOrThrow());
        }

        return Result.Success(values.ToImmutable());
    }
}