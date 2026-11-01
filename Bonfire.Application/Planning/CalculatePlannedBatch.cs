using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Bonfire.Application.Planning;

public sealed class CalculatePlannedBatch(IPlanningContextReader contexts, IRecommendSowing recommendations, TimeProvider timeProvider) : ICalculatePlannedBatch
{
    public Task<Result<PlanningPreviewFromViewModel>> ExecuteAsync(PlanningBatchInputFromViewModel input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.SearchLimitDays < 0) return Task.FromResult(Result.Failure<PlanningPreviewFromViewModel>("Предел поиска не может быть отрицательным"));
        return RegionId.Create(input.RegionId).Bind(region => CultureId.Create(input.CultureId).Bind(culture => SortId.Create(input.SortId)
            .Bind(sort => Result.Success((Region: region, Culture: culture, Sort: sort)))))
            .Match(async ids => await (await contexts.ReadAsync(ids.Region, ids.Culture, ids.Sort, cancellationToken).ConfigureAwait(false))
                .Match(context => Build(input, context, cancellationToken), error => Task.FromResult(Result.Failure<PlanningPreviewFromViewModel>(error))).ConfigureAwait(false),
                error => Task.FromResult(Result.Failure<PlanningPreviewFromViewModel>(error)));
    }

    private Task<Result<PlanningPreviewFromViewModel>> Build(PlanningBatchInputFromViewModel input, PlanningContextFromViewModel context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Region.Definition.IsArchived)
            return Task.FromResult(Result.Failure<PlanningPreviewFromViewModel>("Выберите неархивный регион"));
        var cultureId = CultureId.Create(input.CultureId).ValueOrThrow();
        var culture = context.CultureProfile.Match(x => x, () => CultureGrowingProfile.Create(cultureId, new(), new()).ValueOrThrow());
        var sortExtras = context.SortSettings.Match(x => x.Extras, () => new PlanningExtras());
        var references = new PlanningReferenceSnapshot(context.Culture.Name, context.SortName, context.Region.Definition.Name.Value,
            context.Region.Definition.TimeZoneId, context.RegionalWindows);
        var at = timeProvider.GetUtcNow();
        var missing = GrowingProfileResolver.FindMissingInputs(culture, context.SortCharacteristics, input.Overrides, input.Method is GrowingMethod.DirectSow);
        return Task.FromResult(missing.Bind(items => items.Length > 0
            ? Result.Success(new PlanningPreviewFromViewModel(input, context.Fingerprint, references, Option.None<PlanningBatchDocument>(), items, at))
            : GrowingProfileResolver.Resolve(culture, context.SortCharacteristics, input.Overrides, input.Method is GrowingMethod.DirectSow).Bind(profile =>
            {
                var inherited = GrowingProfileResolver.ResolveExtras(culture.Extras, sortExtras, input.Overrides);
                var extras = inherited with
                {
                    GerminationPercentage = Override(input.GerminationOverride, inherited.GerminationPercentage),
                    LossPercentage = Override(input.LossOverride, inherited.LossPercentage),
                    LunarGroup = Override(input.LunarGroupOverride, inherited.LunarGroup)
                };
                var germination = extras.GerminationPercentage.Map(x => x.Value);
                var losses = extras.LossPercentage.Map(x => x.Value);
                var group = extras.LunarGroup.Match(x => x.Value, () => LunarGroup.Unspecified);
                var settings = new PlanningBatchSettings { Overrides = input.Overrides, Extras = Option.Some(extras),
                    GerminationPercentage = germination, LossPercentage = losses, Group = group };
                return PlannedBatchDefinition.Create(cultureId, SortId.Create(input.SortId).ValueOrThrow(), input.Conditions,
                    input.Method, profile, input.TargetPlants, input.Choices).Bind(batch =>
                    recommendations.Execute(new(input.Method, profile, input.Choices, settings.Extras, group, context.Region.Definition.TimeZone, input.SearchLimitDays), cancellationToken)
                    .Bind(preview => Quantity(input, germination, losses).Bind(quantity =>
                        PlanningBatchDocument.Create(batch, settings, references, preview, quantity, at)
                            .Bind(document => Result.Success(new PlanningPreviewFromViewModel(input, context.Fingerprint, references, Option.Some(document),
                                quantity is Option<SeedQuantityCalculation>.None ? ["Для автоматического количества задайте всхожесть и потери либо введите число семян вручную"] : [], at))))));
            })));
    }

    private static Option<SourcedValue<T>> Override<T>(Option<T> value, Option<SourcedValue<T>> inherited)
        => value.Match(x => Option.Some(new SourcedValue<T>(x, ParameterSource.Batch)), () => inherited);

    private static Result<Option<SeedQuantityCalculation>> Quantity(PlanningBatchInputFromViewModel input,
        Option<GerminationPercentage> germination, Option<LossPercentage> losses)
        => input.Choices.ManualSeedCount.Match(seeds => Result.Success(Option.Some(SeedQuantityCalculation.Manual(input.TargetPlants, seeds, germination, losses))),
            () => germination.Match(g => losses.Match(l => SeedQuantityCalculation.Automatic(input.TargetPlants, g, l)
                .Bind(value => Result.Success(Option.Some(value))), () => Result.Success(Option.None<SeedQuantityCalculation>())),
                () => Result.Success(Option.None<SeedQuantityCalculation>())));
}
