using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedEditorIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fx = new();
    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task SaveSeedEditAsync_SharedSort_UpdatesAllPackagesWithoutRenamingProducer()
    {
        var (firstId, secondId, producerId) = await CreatePackages();
        var service = new SeedsService(_fx.Factory);
        var edit = (await service.GetSeedAsync(firstId))!;
        edit.SeedsInfo.Note = "Новая заметка";
        edit.SeedsInfo.AmountSeeds = 8;
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Черри", Description = "Общее описание" };

        await service.SaveSeedEditAsync(edit, sort, producerId);

        var first = (await service.GetSeedAsync(firstId))!;
        var second = (await service.GetSeedAsync(secondId))!;
        Assert.Equal("Общее описание", second.Plant.PlantSort.Description);
        Assert.Equal(producerId, second.Plant.PlantSort.Producer.Id);
        Assert.Equal("Новый производитель", second.Plant.PlantSort.Producer.Name);
        Assert.Equal("Новая заметка", first.SeedsInfo.Note);
        Assert.Equal(8, first.SeedsInfo.AmountSeeds);
        Assert.Equal("Второй пакет", second.SeedsInfo.Note);
        Assert.Equal(2, await _fx.QueryAsync(db => db.Producers.CountAsync()));
    }

    [Fact]
    public async Task SaveSeedEditAsync_InvalidProducer_DoesNotSavePackageOrSort()
    {
        var (firstId, _, _) = await CreatePackages();
        var service = new SeedsService(_fx.Factory);
        var edit = (await service.GetSeedAsync(firstId))!;
        edit.SeedsInfo.Note = "Не сохранять";
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Черри", Description = "Не сохранять" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveSeedEditAsync(edit, sort, int.MaxValue));

        var saved = (await service.GetSeedAsync(firstId))!;
        Assert.Null(saved.SeedsInfo.Note);
        Assert.Null(saved.Plant.PlantSort.Description);
    }

    private async Task<(int, int, int)> CreatePackages()
    {
        return await _fx.QueryAsync(async db =>
        {
            var plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Томат", Class = "Овощи" },
                PlantSort = new PlantSort { Name = "Черри", Producer = new Producer { Name = "Первый" } }
            };
            var first = new Seed { Plant = plant, SeedsInfo = new SeedsInfo { AmountSeeds = 10 } };
            var second = new Seed { Plant = plant, SeedsInfo = new SeedsInfo { Note = "Второй пакет" } };
            var producer = new Producer { Name = "Новый производитель" };
            db.AddRange(first, second, producer);
            await db.SaveChangesAsync();
            return (first.Id, second.Id, producer.Id);
        });
    }

    [Fact]
    public async Task DeleteSeedWithReasonAsync_PreservesReasonAndOtherPackage()
    {
        var (id, secondId, _) = await CreatePackages();
        var service = new SeedsService(_fx.Factory);
        var infoId = (await service.GetSeedAsync(id))!.SeedsInfo.Id;
        await service.DeleteSeedWithReasonAsync(id, "  Истёк срок годности  ");
        Assert.Null(await service.GetSeedAsync(id));
        Assert.NotNull(await service.GetSeedAsync(secondId));
        var info = await _fx.QueryAsync(db => db.SeedsInfo.SingleAsync(i => i.Id == infoId));
        Assert.Equal("Истёк срок годности", info.DisposeComment);
    }

    [Fact]
    public async Task SaveSeedEditAsync_NewOptions_RemainAvailableAfterReplacingValues()
    {
        var (id, _, producerId) = await CreatePackages();
        var service = new SeedsService(_fx.Factory);
        var edit = (await service.GetSeedAsync(id))!;
        var sort = new SortEditModel
        {
            Id = edit.Plant.PlantSort.Id, Name = "Черри", PlantColor = "Полосатый",
            Maturity = "Среднеранний", GrowingSeason = 70, MaxGrowingSeason = 90, PlantHeight = 50, MaxPlantHeight = 100,
            MinFruitWeight = 10.5, MaxFruitWeight = 20.5, YieldKg = 2.5,
            LandingPattern = 20, LandingRowSpacing = 40,
            MinGerminationTime = 3, MaxGerminationTime = 7, AgeOfSeedlings = 30, MaxAgeOfSeedlings = 40
        };
        await service.SaveSeedEditAsync(edit, sort, producerId);
        var saved = (await service.GetSeedAsync(id))!.Plant.PlantSort;
        Assert.Equal(70, saved.GrowingSeason);
        Assert.Equal(90, saved.MaxGrowingSeason);
        Assert.Equal(100, saved.MaxPlantHeight);
        Assert.Equal(20.5, saved.MaxFruitWeight);
        Assert.Equal(2.5, saved.YieldKg);
        Assert.Equal(40, saved.LandingRowSpacing);
        Assert.Equal(7, saved.MaxGerminationTime);
        Assert.Equal(40, saved.MaxAgeOfSeedlings);
        sort.PlantColor = "Красный";
        sort.Maturity = "Ранний";
        await service.SaveSeedEditAsync(edit, sort, producerId);
        var options = await service.GetEditorOptionsAsync();
        Assert.Contains("Полосатый", options.Colors);
        Assert.Contains("Красный", options.Colors);
        Assert.Contains("Среднеранний", options.Maturities);
        Assert.Equal(2, options.Producers.Count);
    }

    [Fact]
    public async Task UpdateSortAsync_CultureSelection_UpdatesAllPlantsOfSort()
    {
        var (id, _, _) = await CreatePackages();
        var cultureId = await _fx.QueryAsync(async db =>
        {
            var culture = new PlantCulture { Name = "Перец", Class = "Овощи" };
            db.PlantsCulture.Add(culture);
            await db.SaveChangesAsync();
            return culture.Id;
        });
        var sortId = (await new SeedsService(_fx.Factory).GetSeedAsync(id))!.Plant.PlantSort.Id;
        var result = await new LibraryService(_fx.Factory).UpdateSortAsync(
            new SortEditModel { Id = sortId, Name = "Черри", CultureId = cultureId });
        Assert.True(result.IsSuccess);
        var seed = (await new SeedsService(_fx.Factory).GetSeedAsync(id))!;
        Assert.Equal(cultureId, seed.Plant.PlantCulture.Id);
    }
}
