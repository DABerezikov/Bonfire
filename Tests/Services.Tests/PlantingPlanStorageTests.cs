using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using BonfireDB.Entities.Planning;
using BonfireDB.Planning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PlantingPlanStorageTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Save_TwoIndependentBatches_RoundTripPreservesInputsAndHistoricalNames()
    {
        var pair = await PreparePair();
        var store = new DbPlantingPlans(_fixture.Factory);
        var first = Document(pair.Culture, pair.Sort, false);
        var second = Document(pair.Culture, pair.Sort, true);
        var saved = (await store.SaveAsync(NewPlan([NewBatch(first), NewBatch(second)]), default)).ValueOrThrow();
        Assert.Equal(1, saved.Version);
        Assert.Equal(2, saved.Batches.Length);
        Assert.NotEqual(saved.Batches[0].Id, saved.Batches[1].Id);
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.PlantsCulture.SingleAsync()).Name = "Новое имя культуры";
            (await db.PlantsSort.SingleAsync()).MinGerminationTime = 100;
            await db.SaveChangesAsync();
        });
        var loaded = (await store.ReadAsync(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow(), default)).ValueOrThrow()
            .Match(x => x, () => throw new Exception());
        Assert.Equal("Томат", loaded.Batches[0].Document.References.CultureName);
        Assert.Equal(5, loaded.Batches[0].Document.Batch.Profile.Germination.Value.Min);
        Assert.IsType<GrowingMethod.Transplant>(loaded.Batches[0].Document.Batch.Method);
        Assert.IsType<GrowingMethod.DirectSow>(loaded.Batches[1].Document.Batch.Method);
        Assert.Equal(Option.Some(PlantCount.Create(30).ValueOrThrow()), loaded.Batches[0].Document.Batch.Choices.ManualSeedCount);
        Assert.Equal(5, loaded.Batches[0].Document.Settings.Overrides.Germination.Match(x => x.Min, () => -1));
    }

    [Fact]
    public async Task Save_StaleVersionOrForeignBatch_DoesNotOverwritePlan()
    {
        var pair = await PreparePair();
        var store = new DbPlantingPlans(_fixture.Factory);
        var saved = (await store.SaveAsync(NewPlan([NewBatch(Document(pair.Culture, pair.Sort, true))]), default)).ValueOrThrow();
        var edit = PlantingPlanWrite.Create(Option.Some(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow()), saved.Version,
            saved.Definition, saved.Batches.Select(x => new PlannedBatchWrite(Option.Some(PlannedBatchId.Create(x.Id).ValueOrThrow()), x.Document)).ToImmutableArray()).ValueOrThrow();
        Assert.True((await store.SaveAsync(edit, default)).IsSuccess);
        Assert.True((await store.SaveAsync(edit, default)).IsFailure);
        var other = (await store.SaveAsync(NewPlan([]), default)).ValueOrThrow();
        var invalid = PlantingPlanWrite.Create(Option.Some(SeasonalPlantingPlanId.Create(other.Id).ValueOrThrow()), other.Version,
            other.Definition, edit.Batches).ValueOrThrow();
        Assert.True((await store.SaveAsync(invalid, default)).IsFailure);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Set<PlannedBatchRow>().CountAsync()));
    }

    [Fact]
    public async Task Save_DatabaseRejectsSecondBatch_RollsBackPlanAndAllBatches()
    {
        var pair = await PreparePair();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectPlanningBatch BEFORE INSERT ON PlannedBatches
            WHEN NEW.Ordinal = 1 BEGIN SELECT RAISE(ABORT, 'batch rejected'); END;
            """));
        var store = new DbPlantingPlans(_fixture.Factory);
        var request = NewPlan([NewBatch(Document(pair.Culture, pair.Sort, true)), NewBatch(Document(pair.Culture, pair.Sort, false))]);
        var result = await store.SaveAsync(request, default);
        Assert.Contains("batch rejected", result.Match(_ => "нет отказа", error => error));
        Assert.Empty((await store.ReadAllAsync(default)).ValueOrThrow());
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Set<PlannedBatchRow>().CountAsync()));
    }

    [Fact]
    public async Task Read_DeletedLibraryReferences_KeepsSnapshotAndPlanDeletionKeepsInventory()
    {
        var pair = await PreparePair();
        var store = new DbPlantingPlans(_fixture.Factory);
        var saved = (await store.SaveAsync(NewPlan([NewBatch(Document(pair.Culture, pair.Sort, true))]), default)).ValueOrThrow();
        await _fixture.ExecuteAsync(async db =>
        {
            db.Plants.RemoveRange(db.Plants);
            db.PlantsSort.RemoveRange(db.PlantsSort);
            db.PlantsCulture.RemoveRange(db.PlantsCulture);
            await db.SaveChangesAsync();
        });
        var loaded = (await store.ReadAsync(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow(), default)).ValueOrThrow().Match(x => x, () => throw new Exception());
        Assert.IsType<Option<int>.None>(loaded.Batches[0].CurrentCultureId);
        Assert.Equal("Томат", loaded.Batches[0].Document.References.CultureName);
        Assert.True((await store.DeleteAsync(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow(), loaded.Version, default)).IsSuccess);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Set<PlannedBatchRow>().CountAsync()));
    }

    [Fact]
    public async Task Read_UnsupportedSnapshotSchema_ReturnsFailureInsteadOfEmptyPlan()
    {
        var pair = await PreparePair();
        var store = new DbPlantingPlans(_fixture.Factory);
        var saved = (await store.SaveAsync(NewPlan([NewBatch(Document(pair.Culture, pair.Sort, true))]), default)).ValueOrThrow();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.Set<PlannedBatchRow>().SingleAsync()).SnapshotJson = "{\"SchemaVersion\":999}";
            await db.SaveChangesAsync();
        });
        Assert.True((await store.ReadAsync(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow(), default)).IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Snapshot_LunarVariantsAndOperationOverrides_RoundTripWithoutLosingZero(bool conflict)
    {
        var pair = await PreparePair();
        var original = Document(pair.Culture, pair.Sort, false);
        var extras = GrowingProfileResolver.ResolveExtras(new PlanningExtras
        {
            PrickingOut = new OperationOverride<DayRange>.Enabled(DayRange.Create(10, 15).ValueOrThrow()),
            Hardening = new OperationOverride<DayRange>.Disabled(), LossPercentage = Option.Some(LossPercentage.Create(0).ValueOrThrow())
        }, new(), new());
        var settings = original.Settings with { Extras = Option.Some(extras), Group = LunarGroup.AboveGround,
            LossPercentage = Option.Some(LossPercentage.Create(0).ValueOrThrow()) };
        var baseWindow = original.Preview.Agronomy.Sowing;
        var lunar = SowingRecommendations.Calculate(original.Batch.Method, original.Batch.Profile, original.Batch.Choices,
            settings.Extras, settings.Group, date => Result.Success(conflict && baseWindow.Contains(date) ? LunarTrend.Waning : LunarTrend.Growing), 35).ValueOrThrow();
        var document = PlanningBatchDocument.Create(original.Batch, settings, original.References,
            new(original.Preview.Agronomy, lunar), original.Quantity, original.CalculatedAt).ValueOrThrow();
        var stored = (await new DbPlantingPlans(_fixture.Factory).SaveAsync(NewPlan([NewBatch(document)]), default)).ValueOrThrow().Batches[0].Document;
        Assert.Equal(lunar.GetType(), stored.Preview.Lunar.GetType());
        Assert.Equal(0, stored.Settings.LossPercentage.Match(x => x.Value, () => -1));
        Assert.IsType<OperationPlan<DayRange>.Enabled>(stored.Settings.Extras.Match(x => x.PrickingOut, () => throw new Exception()));
        Assert.IsType<OperationPlan<DayRange>.Disabled>(stored.Settings.Extras.Match(x => x.Hardening, () => throw new Exception()));
    }

    [Fact]
    public async Task Read_MalformedSnapshot_ReturnsFailureWithoutThrowingOrInventingValues()
    {
        var pair = await PreparePair();
        var store = new DbPlantingPlans(_fixture.Factory);
        var saved = (await store.SaveAsync(NewPlan([NewBatch(Document(pair.Culture, pair.Sort, true))]), default)).ValueOrThrow();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.Set<PlannedBatchRow>().SingleAsync()).SnapshotJson = "{\"SchemaVersion\":1,\"Document\":{}}";
            await db.SaveChangesAsync();
        });
        Assert.True((await store.ReadAsync(SeasonalPlantingPlanId.Create(saved.Id).ValueOrThrow(), default)).IsFailure);
    }

    private async Task<(int Culture, int Sort)> PreparePair() => await _fixture.QueryAsync(async db =>
    {
        var plant = new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" },
            PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } } };
        db.Plants.Add(plant);
        await db.SaveChangesAsync();
        return (plant.PlantCulture.Id, plant.PlantSort.Id);
    });

    private static PlannedBatchWrite NewBatch(PlanningBatchDocument document) => new(Option.None<PlannedBatchId>(), document);
    private static PlantingPlanWrite NewPlan(ImmutableArray<PlannedBatchWrite> batches) => PlantingPlanWrite.Create(
        Option.None<SeasonalPlantingPlanId>(), 0, SeasonalPlanDefinition.Create("Огород", 2027,
            RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk").ValueOrThrow(), batches).ValueOrThrow();

    internal static PlanningBatchDocument Document(int culture, int sort, bool direct)
    {
        var g = new SourcedValue<DayRange>(DayRange.Create(5, 10).ValueOrThrow(), ParameterSource.Batch);
        var a = new SourcedValue<DayRange>(DayRange.Create(50, 60).ValueOrThrow(), ParameterSource.Sort);
        var profile = new EffectiveGrowingProfile(g, direct ? Option.None<SourcedValue<DayRange>>() : Option.Some(a), Option.None<SourcedValue<DayRange>>());
        var window = DateWindow.Create(new(2027, 5, 20), new(2027, 5, 25)).ValueOrThrow();
        GrowingMethod method = direct ? new GrowingMethod.DirectSow(window) : new GrowingMethod.Transplant(window);
        var choices = PlanningChoices.Empty with { ManualSeedCount = Option.Some(PlantCount.Create(30).ValueOrThrow()) };
        var batch = PlannedBatchDefinition.Create(CultureId.Create(culture).ValueOrThrow(), SortId.Create(sort).ValueOrThrow(),
            GrowingConditions.Create(GrowingLocation.OpenGround, PlantPlacement.InGround).ValueOrThrow(), method,
            profile, PlantCount.Create(20).ValueOrThrow(), choices).ValueOrThrow();
        var settings = new PlanningBatchSettings { Overrides = new() { Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow()) } };
        var schedule = PlantingSchedule.Calculate(method, profile, choices, settings.Extras).ValueOrThrow();
        return PlanningBatchDocument.Create(batch, settings, new("Томат", "Черри", "Томск", "Asia/Tomsk", []),
            new(schedule, new SowingRecommendations.Unavailable(LunarUnavailableReason.UnspecifiedGroup, "Не задана группа")),
            Option.Some(SeedQuantityCalculation.Manual(batch.TargetPlants, PlantCount.Create(30).ValueOrThrow(),
                Option.None<GerminationPercentage>(), Option.None<LossPercentage>())), new DateTimeOffset(2026, 11, 1, 6, 0, 0, TimeSpan.Zero)).ValueOrThrow();
    }
}
