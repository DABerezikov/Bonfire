using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Planning.GrowingProfiles;
using BonfireDB.Planning.Libraries;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PlanningLibrariesIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Regions_TomskSeedAndEdit_RoundTripKeepsEmptyNoteAndArchive()
    {
        var store = new DbPlanningLibraries(_fixture.Factory);
        var initial = (await store.ReadRegionsAsync(true, default)).ValueOrThrow();
        var tomsk = Assert.Single(initial);
        Assert.Equal("Томск", tomsk.Definition.Name.Value);
        Assert.Equal("Asia/Tomsk", tomsk.Definition.TimeZoneId);
        Assert.IsType<Option<AnnualDateWindow>.None>(tomsk.Definition.Reference.LastSpringFrost);
        var frost = AnnualDateWindow.Create(5, 15, 6, 10, false).ValueOrThrow();
        var edit = RegionDefinition.Create(" Томск ", "Asia/Tomsk", RegionReference.Empty with
        {
            LastSpringFrost = Option.Some(frost), Note = Option.Some(""), Source = Option.Some("Ручной ввод")
        }, true).ValueOrThrow();
        Assert.True((await store.SaveRegionAsync(Option.Some(tomsk.Id), edit, default)).IsSuccess);
        Assert.Empty((await store.ReadRegionsAsync(false, default)).ValueOrThrow());
        var saved = Assert.Single((await store.ReadRegionsAsync(true, default)).ValueOrThrow());
        Assert.Equal(edit.Reference, saved.Definition.Reference);
        Assert.Equal("Томск", saved.Definition.Name.Value);
    }

    [Fact]
    public async Task RegionalWindows_UpsertByFullKey_DistinguishesPurposeAndConditions()
    {
        var culture = await _fixture.PrepareAsync(new PlantCulture { Name = "Томат", Class = "Овощи" });
        var store = new DbPlanningLibraries(_fixture.Factory);
        var region = RegionId.Create(1).ValueOrThrow();
        var cultureId = CultureId.Create(culture.Id).ValueOrThrow();
        var conditions = GrowingConditions.Create(GrowingLocation.OpenGround, PlantPlacement.InGround).ValueOrThrow();
        var first = AnnualDateWindow.Create(5, 20, 6, 10, false).ValueOrThrow();
        var second = AnnualDateWindow.Create(6, 1, 6, 15, false).ValueOrThrow();
        foreach (var purpose in Enum.GetValues<RegionalWindowPurpose>())
        {
            var value = RegionalPlantingWindow.Create(region, cultureId, conditions, purpose, first, Option.None<string>(), Option.Some("")).ValueOrThrow();
            Assert.True((await store.SaveRegionalWindowAsync(value, default)).IsSuccess);
            Assert.True((await store.SaveRegionalWindowAsync(RegionalPlantingWindow.Create(region, cultureId, conditions,
                purpose, second, Option.None<string>(), Option.Some("")).ValueOrThrow(), default)).IsSuccess);
        }
        var rows = (await store.ReadRegionalWindowsAsync(region, cultureId, default)).ValueOrThrow();
        Assert.Equal(2, rows.Length);
        Assert.All(rows, value => Assert.Equal(second, value.Window));
        Assert.All(rows, value => Assert.Equal(Option.Some(""), value.Note));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Set<RegionalPlantingWindowRow>().CountAsync()));
    }

    [Fact]
    public async Task Profiles_RoundTripExtras_DoesNotOverwriteExistingSortFields()
    {
        var culture = await _fixture.PrepareAsync(new PlantCulture { Name = "Томат", Class = "Овощи" });
        var sort = await _fixture.PrepareAsync(new PlantSort
        {
            Name = "Сорт", Producer = new Producer { Name = "Фирма" },
            MinGerminationTime = 3, MaxGerminationTime = 8, YieldKg = 7
        });
        var store = new DbPlanningLibraries(_fixture.Factory);
        var extras = new PlanningExtras
        {
            PrickingOut = new OperationOverride<DayRange>.Disabled(),
            Hardening = new OperationOverride<DayRange>.Enabled(DayRange.Create(7, 10).ValueOrThrow()),
            GerminationPercentage = Option.Some(GerminationPercentage.Create(80.5m).ValueOrThrow()),
            LossPercentage = Option.Some(LossPercentage.Create(0).ValueOrThrow()),
            LunarGroup = Option.Some(LunarGroup.AboveGround), ReferenceSoilTemperatureC = Option.Some(12.5m)
        };
        var profile = CultureGrowingProfile.Create(CultureId.Create(culture.Id).ValueOrThrow(),
            new GrowingProfileParameters { Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow()) }, extras).ValueOrThrow();
        var settings = SortPlanningSettings.Create(SortId.Create(sort.Id).ValueOrThrow(), extras).ValueOrThrow();
        Assert.True((await store.SaveCultureProfileAsync(profile, default)).IsSuccess);
        Assert.True((await store.SaveCultureProfileAsync(profile, default)).IsSuccess);
        Assert.True((await store.SaveSortSettingsAsync(settings, default)).IsSuccess);
        var loaded = (await store.ReadProfilesAsync(profile.CultureId, settings.SortId, default)).ValueOrThrow();
        Assert.Equal(profile, loaded.Culture.Match(x => x, () => throw new Exception()));
        Assert.Equal(settings, loaded.Sort.Match(x => x, () => throw new Exception()));
        var original = await _fixture.LoadAsync<PlantSort>(sort.Id);
        Assert.Equal(3, original!.MinGerminationTime);
        Assert.Equal(8, original.MaxGerminationTime);
        Assert.Equal(7, original.YieldKg);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Set<CultureGrowingProfileRow>().CountAsync()));
    }

    [Fact]
    public async Task Save_UnknownForeignKeyOrRejectedWrite_ReturnsFailureWithoutPartialChanges()
    {
        var store = new DbPlanningLibraries(_fixture.Factory);
        var profile = CultureGrowingProfile.Create(CultureId.Create(999).ValueOrThrow(), new(), new()).ValueOrThrow();
        Assert.True((await store.SaveCultureProfileAsync(profile, default)).IsFailure);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectRegion BEFORE UPDATE ON PlanningRegions BEGIN SELECT RAISE(ABORT, 'region rejected'); END;"));
        var result = await store.SaveRegionAsync(Option.Some(RegionId.Create(1).ValueOrThrow()),
            RegionDefinition.Create("Изменение", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow(), default);
        Assert.Contains("region rejected", result.Match(_ => "нет отказа", error => error));
        Assert.Equal("Томск", Assert.Single((await store.ReadRegionsAsync(true, default)).ValueOrThrow()).Definition.Name.Value);
    }
}
