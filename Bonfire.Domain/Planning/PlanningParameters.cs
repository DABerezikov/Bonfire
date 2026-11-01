using System.Diagnostics;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public enum ParameterSource { Culture, Sort, Batch }
public sealed record SourcedValue<T>(T Value, ParameterSource Source);
public sealed record RangeInput(Option<int> Min, Option<int> Max);

public abstract record OperationOverride<T> where T : notnull
{
    private OperationOverride() { }
    public sealed record Inherit : OperationOverride<T>;
    public sealed record Disabled : OperationOverride<T>;
    public sealed record Enabled : OperationOverride<T>
    {
        public T Settings { get; }
        public Enabled(T settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            Settings = settings;
        }
    }

    public Option<SourcedValue<T>> Resolve(Option<SourcedValue<T>> inherited) => this switch
    {
        Inherit => inherited,
        Disabled => Option.None<SourcedValue<T>>(),
        Enabled enabled => Option.Some(new SourcedValue<T>(enabled.Settings, ParameterSource.Batch)),
        _ => throw new UnreachableException()
    };
}

public static class PlanningParameters
{
    /// <summary>Диапазон наследуется целиком. Неполное значение сорта требует уточнения.</summary>
    public static Result<SourcedValue<DayRange>> ResolveRange(string name, Option<DayRange> batch,
        RangeInput sort, RangeInput culture)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(sort);
        ArgumentNullException.ThrowIfNull(culture);
        return batch.Match(
            value => Result.Success(new SourcedValue<DayRange>(value, ParameterSource.Batch)),
            () => sort is { Min: Option<int>.None, Max: Option<int>.None }
                ? Complete(name, culture, ParameterSource.Culture)
                : Complete(name, sort, ParameterSource.Sort));
    }

    private static Result<SourcedValue<DayRange>> Complete(string name, RangeInput input, ParameterSource source)
        => input is { Min: Option<int>.Some min, Max: Option<int>.Some max }
            ? DayRange.Create(min.Value, max.Value).Bind(range => Result.Success(new SourcedValue<DayRange>(range, source)))
            : Result.Failure<SourcedValue<DayRange>>($"Не задан полный диапазон «{name}» ({source})");
}
