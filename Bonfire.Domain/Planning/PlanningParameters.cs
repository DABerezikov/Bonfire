using System.Diagnostics;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public enum ParameterSource
{
    Culture,
    Sort,
    Batch
}

public sealed record SourcedValue<T>(T Value, ParameterSource Source);

public sealed record RangeInput(Option<int> Min, Option<int> Max);

public abstract record PlanningRangeResolution
{
    private PlanningRangeResolution()
    {
    }

    public sealed record Complete(SourcedValue<DayRange> Value) : PlanningRangeResolution;

    public sealed record Missing(string Message) : PlanningRangeResolution;
}

public abstract record OperationOverride<T> where T : notnull
{
    private OperationOverride()
    {
    }

    public Option<SourcedValue<T>> Resolve(Option<SourcedValue<T>> inherited) => this switch
    {
        Inherit => inherited,
        Disabled => Option.None<SourcedValue<T>>(),
        Enabled enabled => Option.Some(new SourcedValue<T>(enabled.Settings, ParameterSource.Batch)),
        _ => throw new UnreachableException()
    };

    public sealed record Inherit : OperationOverride<T>;

    public sealed record Disabled : OperationOverride<T>;

    public sealed record Enabled : OperationOverride<T>
    {
        public Enabled(T settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            Settings = settings;
        }

        public T Settings { get; }
    }
}

public static class PlanningParameters
{
    /// <summary>Диапазон наследуется целиком. Неполное значение сорта требует уточнения.</summary>
    public static Result<SourcedValue<DayRange>> ResolveRange(string name, Option<DayRange> batch,
        RangeInput sort, RangeInput culture)
        => ResolveRangeStatus(name, batch, sort, culture).Bind(status => status switch
        {
            PlanningRangeResolution.Complete complete => Result.Success(complete.Value),
            PlanningRangeResolution.Missing missing => Result.Failure<SourcedValue<DayRange>>(missing.Message),
            _ => throw new UnreachableException()
        });

    public static Result<PlanningRangeResolution> ResolveRangeStatus(string name, Option<DayRange> batch,
        RangeInput sort, RangeInput culture)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(sort);
        ArgumentNullException.ThrowIfNull(culture);
        return batch.Match(
            value => Result.Success<PlanningRangeResolution>(
                new PlanningRangeResolution.Complete(new SourcedValue<DayRange>(value, ParameterSource.Batch))),
            () => sort is { Min: Option<int>.None, Max: Option<int>.None }
                ? Complete(name, culture, ParameterSource.Culture)
                : Complete(name, sort, ParameterSource.Sort));
    }

    private static Result<PlanningRangeResolution> Complete(string name, RangeInput input, ParameterSource source)
        => input.Min.Match(value => value < 0, () => false) || input.Max.Match(value => value < 0, () => false)
            ? Result.Failure<PlanningRangeResolution>($"Отрицательная длительность «{name}» ({source})")
            : input is { Min: Option<int>.Some min, Max: Option<int>.Some max }
                ? DayRange.Create(min.Value, max.Value).Bind(range =>
                    Result.Success<PlanningRangeResolution>(
                        new PlanningRangeResolution.Complete(new SourcedValue<DayRange>(range, source))))
                : Result.Success<PlanningRangeResolution>(
                    new PlanningRangeResolution.Missing($"Не задан полный диапазон «{name}» ({source})"));
}