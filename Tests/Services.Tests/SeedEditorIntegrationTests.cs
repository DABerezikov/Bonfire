using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Models.Mappers;

namespace Services.Tests;

public class SeedEditorIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fx = new();
    public void Dispose() => _fx.Dispose();

    private static Task<Result<SeedEditResultFromViewModel>> SaveAsync(SeedsService service, Seed edited, SortEditModel sort, int producer)
        => SeedEditMapper.ToRequest(SeedEditMapper.FromSeed(edited), sort, producer).Match(
            service.SaveSeedEditAsync,
            error => Task.FromResult(Result.Failure<SeedEditResultFromViewModel>(error)));

    [Fact]
    public async Task SaveSeedEditAsync_SharedSort_UpdatesAllPackagesWithoutRenamingProducer()
    {
        var (firstId, secondId, producerId) = await CreatePackages();
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(firstId))!;
        edit.SeedsInfo.Note = "Новая заметка";
        edit.SeedsInfo.AmountSeeds = 8;
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Черри", Description = "Общее описание" };

        await SaveAsync(service, edit, sort, producerId);

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
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(firstId))!;
        edit.SeedsInfo.Note = "Не сохранять";
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Черри", Description = "Не сохранять" };

        Assert.True((await SaveAsync(service, edit, sort, int.MaxValue)).IsFailure);

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
    public async Task SaveSeedEditAsync_RejectedSave_RollsBackPackageSortProducerAndOptions()
    {
        var (id, second, producer) = await CreatePackages();
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(id))!;
        edit.SeedsInfo.AmountSeeds = 2;
        edit.SeedsInfo.Note = "Не сохранять";
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Новое", PlantColor = "Новый цвет" };
        await _fx.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectPackage BEFORE UPDATE ON SeedsInfo BEGIN SELECT RAISE(ABORT, 'package rejected'); END;"));
        var result = await SaveAsync(service, edit, sort, producer);
        Assert.Contains("package rejected", result.Match(_ => "не ожидалось", error => error));
        var saved = (await service.GetSeedAsync(id))!;
        Assert.Equal(10, saved.SeedsInfo.AmountSeeds);
        Assert.Null(saved.SeedsInfo.Note);
        Assert.Equal("Черри", saved.Plant.PlantSort.Name);
        Assert.Equal("Первый", saved.Plant.PlantSort.Producer.Name);
        Assert.Equal(0, await _fx.QueryAsync(db => db.SortOptions.CountAsync()));
        Assert.Equal("Второй пакет", (await service.GetSeedAsync(second))!.SeedsInfo.Note);
    }

    [Fact]
    public async Task SaveSeedEditAsync_OptionalValues_PreservesNullEmptyZeroAndRawText()
    {
        var (id, _, producer) = await CreatePackages();
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(id))!;
        edit.SeedsInfo.WeightPack = .8; edit.SeedsInfo.QuantityPack = 10;
        edit.SeedsInfo.AmountSeeds = 0; edit.SeedsInfo.AmountSeedsWeight = null;
        edit.SeedsInfo.CostPack = 12.34m;
        edit.SeedsInfo.PurchaseDate = new DateTime(2025, 1, 2);
        edit.SeedsInfo.ExpirationDate = new DateTime(2028, 12, 31);
        edit.SeedsInfo.Note = ""; edit.SeedsInfo.SeedSource = " Куплено "; edit.SeedsInfo.DisposeComment = null;
        var sort = new SortEditModel
        {
            Id = edit.Plant.PlantSort.Id, Name = " Черри ", Description = "",
            MinGerminationTime = null, MaxGerminationTime = 0,
            AgeOfSeedlings = null, MaxAgeOfSeedlings = 0, PlantColor = " Полосатый ", Maturity = null,
            GrowingSeason = 0, MaxGrowingSeason = null, LandingPattern = 0, LandingRowSpacing = null,
            PlantHeight = null, MaxPlantHeight = 0, YieldKg = null, MinFruitWeight = 0, MaxFruitWeight = null,
            CultureId = int.MaxValue
        };
        await SaveAsync(service, edit, sort, producer);
        var saved = (await service.GetSeedAsync(id))!;
        Assert.True(Bonfire.Models.Mappers.SeedMapper.SamePackage(edit.SeedsInfo, saved.SeedsInfo));
        Assert.Equal(" Черри ", saved.Plant.PlantSort.Name);
        Assert.Equal("", saved.Plant.PlantSort.Description);
        Assert.Null(saved.Plant.PlantSort.MinGerminationTime);
        Assert.Equal(0, saved.Plant.PlantSort.MaxGerminationTime);
        Assert.Null(saved.Plant.PlantSort.AgeOfSeedlings);
        Assert.Equal(0, saved.Plant.PlantSort.MaxAgeOfSeedlings);
        Assert.Equal(" Полосатый ", saved.Plant.PlantSort.PlantColor);
        Assert.Null(saved.Plant.PlantSort.Maturity);
        Assert.Equal("Томат", saved.Plant.PlantCulture.Name);
        Assert.Contains("Полосатый", (await service.GetEditorOptionsAsync()).Colors);
    }

    [Fact]
    public async Task DeleteSeedWithReasonAsync_PreservesReasonAndOtherPackage()
    {
        var (id, secondId, _) = await CreatePackages();
        var service = _fx.CreateSeedsService();
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
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(id))!;
        var sort = new SortEditModel
        {
            Id = edit.Plant.PlantSort.Id, Name = "Черри", PlantColor = "Полосатый",
            Maturity = "Среднеранний", GrowingSeason = 70, MaxGrowingSeason = 90, PlantHeight = 50, MaxPlantHeight = 100,
            MinFruitWeight = 10.5, MaxFruitWeight = 20.5, YieldKg = 2.5,
            LandingPattern = 20, LandingRowSpacing = 40,
            MinGerminationTime = 3, MaxGerminationTime = 7, AgeOfSeedlings = 30, MaxAgeOfSeedlings = 40
        };
        await SaveAsync(service, edit, sort, producerId);
        var saved = (await service.GetSeedAsync(id))!.Plant.PlantSort;
        Assert.Equal(70, saved.GrowingSeason);
        Assert.Equal(50, saved.PlantHeight);
        Assert.Equal(10.5, saved.MinFruitWeight);
        Assert.Equal(20, saved.LandingPattern);
        Assert.Equal(3, saved.MinGerminationTime);
        Assert.Equal(30, saved.AgeOfSeedlings);
        Assert.Equal(90, saved.MaxGrowingSeason);
        Assert.Equal(100, saved.MaxPlantHeight);
        Assert.Equal(20.5, saved.MaxFruitWeight);
        Assert.Equal(2.5, saved.YieldKg);
        Assert.Equal(40, saved.LandingRowSpacing);
        Assert.Equal(7, saved.MaxGerminationTime);
        Assert.Equal(40, saved.MaxAgeOfSeedlings);
        sort.PlantColor = "Красный";
        sort.Maturity = "Ранний";
        await SaveAsync(service, edit, sort, producerId);
        var options = await service.GetEditorOptionsAsync();
        Assert.Contains("Полосатый", options.Colors);
        Assert.Contains("Красный", options.Colors);
        Assert.Contains("Среднеранний", options.Maturities);
        Assert.Equal(2, options.Producers.Count);
        Assert.Equal(1, await _fx.QueryAsync(db => db.SortOptions.CountAsync(o => o.Category == "Цвет" && o.Name == "Полосатый")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveSeedEditAsync_MissingOrChangedSeed_ReturnsFailureWithoutWriting(bool missing)
    {
        var (id, _, producer) = await CreatePackages();
        var service = _fx.CreateSeedsService();
        var edit = (await service.GetSeedAsync(id))!;
        edit.SeedsInfo.Note = "Не сохранять";
        var sort = new SortEditModel { Id = edit.Plant.PlantSort.Id, Name = "Новое", PlantColor = "Не сохранять" };
        if (missing) edit.Id = int.MaxValue;
        else sort = new() { Id = int.MaxValue, Name = "Новое", PlantColor = "Не сохранять" };
        var result = await SaveAsync(service, edit, sort, producer);
        Assert.Contains(missing ? "Запись семян удалена" : "Сорт записи изменён", result.Match(_ => "не ожидалось", error => error));
        var saved = (await service.GetSeedAsync(id))!;
        Assert.Null(saved.SeedsInfo.Note);
        Assert.Equal("Черри", saved.Plant.PlantSort.Name);
        Assert.Equal(0, await _fx.QueryAsync(db => db.SortOptions.CountAsync()));
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
        var sortId = (await _fx.CreateSeedsService().GetSeedAsync(id))!.Plant.PlantSort.Id;
        var result = await _fx.CreateLibraryService().UpdateSortAsync(
            new SortEditModel { Id = sortId, Name = "Черри", CultureId = cultureId });
        Assert.True(result.IsSuccess);
        var seed = (await _fx.CreateSeedsService().GetSeedAsync(id))!;
        Assert.Equal(cultureId, seed.Plant.PlantCulture.Id);
    }
}
