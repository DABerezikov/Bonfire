using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class PlanningApplicationTests
{
    private readonly IPlanningContextReader _contexts = Substitute.For<IPlanningContextReader>();
    private readonly ILunarTrendCalendar _calendar = Substitute.For<ILunarTrendCalendar>();

    public PlanningApplicationTests()
    {
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Context()));
        _calendar.GetTrend(Arg.Any<DateOnly>(), Arg.Any<TimeZoneInfo>()).Returns(Result.Success(LunarTrend.Growing));
    }

    [Fact]
    public async Task Calculate_CompleteInput_ResolvesSourcesAndQuantityWithoutWriting()
    {
        var preview = (await Calculator().ExecuteAsync(Input())).ValueOrThrow();
        var document = preview.Document.Match(x => x, () => throw new Exception());
        Assert.Empty(preview.MissingInputs);
        Assert.Equal(28, document.Quantity.Match(x => x.Seeds.Value, () => -1));
        Assert.Equal(ParameterSource.Sort, document.Batch.Profile.Germination.Source);
        Assert.Equal("Томск", document.References.RegionName);
        Assert.Equal(2, document.References.RegionalWindows.Length);
        Assert.IsType<SowingRecommendations.Available>(document.Preview.Lunar);
    }

    [Fact]
    public async Task Calculate_MissingAndPartialRanges_ReturnsIncompletePreviewWithReference()
    {
        var context = Context() with { SortCharacteristics = new(), CultureProfile = Option.None<CultureGrowingProfile>() };
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>()).Returns(Result.Success(context));
        var preview = (await Calculator().ExecuteAsync(Input())).ValueOrThrow();
        Assert.IsType<Option<PlanningBatchDocument>.None>(preview.Document);
        Assert.NotEmpty(preview.MissingInputs);
        Assert.Equal(2, preview.References.RegionalWindows.Length);
        Assert.Empty(_calendar.ReceivedCalls());
    }

    [Fact]
    public async Task Calculate_DirectSowWithoutAge_PreservesManualDateAndCount()
    {
        var input = Input() with { Method = new GrowingMethod.DirectSow(Window()),
            Choices = PlanningChoices.Empty with { SowingDate = Option.Some(new DateOnly(2027, 5, 1)),
                ManualSeedCount = Option.Some(PlantCount.Create(7).ValueOrThrow()) } };
        var document = (await Calculator().ExecuteAsync(input)).ValueOrThrow().Document.Match(x => x, () => throw new Exception());
        Assert.Equal(7, document.Quantity.Match(x => x.Seeds.Value, () => -1));
        Assert.Equal(input.Choices, document.Batch.Choices);
        Assert.IsType<Option<SourcedValue<DayRange>>.None>(document.Batch.Profile.SeedlingAge);
        Assert.IsType<Option<DateWindow>.None>(document.Preview.Agronomy.Transplant);
    }

    [Fact]
    public async Task Calculate_PartialSortRange_DoesNotBorrowCultureBound_AndNegativeValueIsFailure()
    {
        var context = Context();
        var culture = context.CultureProfile.Match(x => x, () => throw new Exception());
        context = context with
        {
            SortCharacteristics = context.SortCharacteristics with { MaxGerminationTime = Option.None<int>() },
            CultureProfile = Option.Some(CultureGrowingProfile.Create(culture.CultureId,
                new() { Germination = Option.Some(DayRange.Create(7, 14).ValueOrThrow()) }, culture.Extras).ValueOrThrow())
        };
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>()).Returns(Result.Success(context));
        var preview = (await Calculator().ExecuteAsync(Input())).ValueOrThrow();
        Assert.IsType<Option<PlanningBatchDocument>.None>(preview.Document);
        Assert.Contains(preview.MissingInputs, x => x.Contains("Прорастание"));
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(context with { SortCharacteristics = context.SortCharacteristics with { MinGerminationTime = Option.Some(-1) } }));
        Assert.True((await Calculator().ExecuteAsync(Input())).IsFailure);
    }

    [Fact]
    public async Task Save_ChangedInputOrReference_DoesNotWrite()
    {
        var reader = Substitute.For<IPlantingPlanReader>();
        var writer = Substitute.For<IPlantingPlanWriter>();
        var libraries = Substitute.For<IPlanningLibraryReader>();
        var operations = new ManagePlantingPlans(reader, writer, _contexts, libraries);
        var input = Input();
        var preview = (await Calculator().ExecuteAsync(input)).ValueOrThrow();
        var changed = input with { TargetPlants = PlantCount.Create(99).ValueOrThrow() };
        Assert.True((await operations.SaveBatchAsync(new(1, 1, Option.None<int>(), changed, preview, false))).IsFailure);
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Context() with { Fingerprint = "changed" }));
        Assert.True((await operations.SaveBatchAsync(new(1, 1, Option.None<int>(), input, preview, false))).IsFailure);
        Assert.Empty(writer.ReceivedCalls());
    }

    [Fact]
    public async Task Calculate_CalendarFailure_KeepsAgronomy_ArchivedRegionIsRejected()
    {
        _calendar.GetTrend(Arg.Any<DateOnly>(), Arg.Any<TimeZoneInfo>()).Returns(Result.Failure<LunarTrend>("Ошибка календаря"));
        var preview = (await Calculator().ExecuteAsync(Input())).ValueOrThrow();
        Assert.IsType<SowingRecommendations.Unavailable>(preview.Document.Match(x => x.Preview.Lunar, () => throw new Exception()));
        var context = Context();
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(context with { Region = context.Region with { Definition = RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty, true).ValueOrThrow() } }));
        Assert.True((await Calculator().ExecuteAsync(Input())).IsFailure);
    }

    private CalculatePlannedBatch Calculator() => new(_contexts, new RecommendSowing(_calendar), TimeProvider.System);
    [Fact]
    public async Task HarvestTarget_InheritsIndependentNormsAndCalculatesSeedsForRequiredPlants()
    {
        var context = Context();
        var profile = context.CultureProfile.Match(x => x, () => throw new Exception());
        _contexts.ReadAsync(Arg.Any<RegionId>(), Arg.Any<CultureId>(), Arg.Any<SortId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(context with { CultureProfile = Option.Some(CultureGrowingProfile.Create(profile.CultureId, profile.Parameters,
                profile.Extras with { YieldPerPlantKg = Option.Some(2m), YieldPerM2Kg = Option.Some(4m) }).ValueOrThrow()) }));
        var input = Input() with { Harvest = new() { Mode = HarvestPlanningMode.TargetPerPlant, TargetKg = Option.Some(15m),
            YieldAreaM2 = Option.Some(1.8m), Layout = Option.Some(new BedLayout(2m, .9m, .4m, .6m, .2m, .2m, .3m, .3m, false)) } };
        var preview = (await Calculator().ExecuteAsync(input)).ValueOrThrow();
        var document = preview.Document.Match(x => x, () => throw new Exception());
        Assert.Equal(input, preview.Input);
        Assert.Equal(8, document.Batch.TargetPlants.Value);
        Assert.Equal(12, document.Quantity.Match(x => x.Seeds.Value, () => -1));
        Assert.Equal(ParameterSource.Culture, document.Settings.Harvest.PerPlantSource);
        Assert.Equal(Option.Some(3), HarvestProjection.Calculate(document.Settings.Harvest, document.Batch.TargetPlants.Value).ValueOrThrow().PlantDeficit);
    }
    private static DateWindow Window() => DateWindow.Create(new(2027, 5, 20), new(2027, 5, 25)).ValueOrThrow();
    internal static PlanningBatchInputFromViewModel Input() => new(1, 1, 2,
        GrowingConditions.Create(GrowingLocation.OpenGround, PlantPlacement.InGround).ValueOrThrow(), new GrowingMethod.Transplant(Window()),
        PlantCount.Create(20).ValueOrThrow(), new(), PlanningChoices.Empty, Option.None<GerminationPercentage>(),
        Option.None<LossPercentage>(), Option.None<LunarGroup>(), 35);

    private static PlanningContextFromViewModel Context()
    {
        var regionId = RegionId.Create(1).ValueOrThrow();
        var cultureId = CultureId.Create(1).ValueOrThrow();
        var extras = new PlanningExtras { GerminationPercentage = Option.Some(GerminationPercentage.Create(80).ValueOrThrow()),
            LossPercentage = Option.Some(LossPercentage.Create(10).ValueOrThrow()), LunarGroup = Option.Some(LunarGroup.AboveGround) };
        var windows = new[] { GrowingLocation.OpenGround, GrowingLocation.Greenhouse }.Select(location =>
            RegionalPlantingWindow.Create(regionId, cultureId, GrowingConditions.Create(location, PlantPlacement.InGround).ValueOrThrow(),
                RegionalWindowPurpose.Transplanting, AnnualDateWindow.Create(5, 1, 6, 10, false).ValueOrThrow(),
                Option.Some("Ручная справка"), Option.None<string>()).ValueOrThrow()).ToImmutableArray();
        return new(new(regionId, RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow()),
            new(1, "Томат", "Овощи"), 2, "Черри", new SortCharacteristicsValues
            {
                MinGerminationTime = Option.Some(5), MaxGerminationTime = Option.Some(10),
                AgeOfSeedlings = Option.Some(50), MaxAgeOfSeedlings = Option.Some(60)
            }, Option.Some(CultureGrowingProfile.Create(cultureId, new(), extras).ValueOrThrow()),
            Option.None<SortPlanningSettings>(), windows, "v1");
    }
}
