using System.Diagnostics;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Domain.Planning.GrowingProfiles;

public static class GrowingProfileResolver
{
    public static Result<EffectiveGrowingProfile> Resolve(CultureGrowingProfile culture,
        SortCharacteristicsValues sort, PlanningOverrides overrides, bool directSow)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(sort);
        ArgumentNullException.ThrowIfNull(overrides);
        return PlanningParameters.ResolveRange("Прорастание", overrides.Germination,
            new(sort.MinGerminationTime, sort.MaxGerminationTime), ToInput(culture.Parameters.Germination))
            .Bind(germination => ResolveOptional("Вегетация от всходов", overrides.Vegetation,
                new(sort.GrowingSeason, sort.MaxGrowingSeason), culture.Parameters.Vegetation)
                .Bind(vegetation => directSow
                    ? Result.Success(new EffectiveGrowingProfile(germination, Option.None<SourcedValue<DayRange>>(), vegetation))
                    : PlanningParameters.ResolveRange("Возраст рассады от всходов", overrides.SeedlingAge,
                        new(sort.AgeOfSeedlings, sort.MaxAgeOfSeedlings), ToInput(culture.Parameters.SeedlingAge))
                        .Bind(age => Result.Success(new EffectiveGrowingProfile(germination, Option.Some(age), vegetation)))));
    }

    public static EffectivePlanningExtras ResolveExtras(PlanningExtras culture, PlanningExtras sort, PlanningOverrides batch)
        => new(ResolveOperation(batch.PrickingOut, ParameterSource.Batch,
                ResolveOperation(sort.PrickingOut, ParameterSource.Sort,
                    ResolveOperation(culture.PrickingOut, ParameterSource.Culture, new OperationPlan<DayRange>.Unknown()))),
            ResolveOperation(batch.Hardening, ParameterSource.Batch,
                ResolveOperation(sort.Hardening, ParameterSource.Sort,
                    ResolveOperation(culture.Hardening, ParameterSource.Culture, new OperationPlan<DayRange>.Unknown()))),
            ResolveValue(sort.GerminationPercentage, culture.GerminationPercentage),
            ResolveValue(sort.LossPercentage, culture.LossPercentage),
            ResolveValue(sort.LunarGroup, culture.LunarGroup),
            ResolveValue(sort.ReferenceSoilTemperatureC, culture.ReferenceSoilTemperatureC));

    private static OperationPlan<T> ResolveOperation<T>(OperationOverride<T> value, ParameterSource source, OperationPlan<T> inherited)
        where T : notnull => value switch
        {
            OperationOverride<T>.Inherit => inherited,
            OperationOverride<T>.Disabled => new OperationPlan<T>.Disabled(source),
            OperationOverride<T>.Enabled enabled => new OperationPlan<T>.Enabled(new(enabled.Settings, source)),
            _ => throw new UnreachableException()
        };

    private static Option<SourcedValue<T>> ResolveValue<T>(Option<T> sort, Option<T> culture)
        => sort.Match(value => Option.Some(new SourcedValue<T>(value, ParameterSource.Sort)),
            () => culture.Map(value => new SourcedValue<T>(value, ParameterSource.Culture)));

    private static Result<Option<SourcedValue<DayRange>>> ResolveOptional(string name, Option<DayRange> batch,
        RangeInput sort, Option<DayRange> culture)
        => batch is Option<DayRange>.None && sort is { Min: Option<int>.None, Max: Option<int>.None }
            && culture is Option<DayRange>.None
                ? Result.Success(Option.None<SourcedValue<DayRange>>())
                : PlanningParameters.ResolveRange(name, batch, sort, ToInput(culture))
                    .Bind(value => Result.Success(Option.Some(value)));

    private static RangeInput ToInput(Option<DayRange> range)
        => range.Match(value => new RangeInput(Option.Some(value.Min), Option.Some(value.Max)),
            () => new RangeInput(Option.None<int>(), Option.None<int>()));
}
