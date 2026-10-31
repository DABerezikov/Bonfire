using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Libraries;

namespace Services.Tests;

public class LibraryReadingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Read_SavedLibrary_PreservesSharedIdsTextAndAllCharacteristics()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var sort = new PlantSort { Name = "  Сорт  ", Producer = new() { Name = "Фирма" }, Description = "",
                MinGerminationTime = 1, MaxGerminationTime = 2, AgeOfSeedlings = 3, MaxAgeOfSeedlings = 4,
                GrowingSeason = 5, MaxGrowingSeason = 6, LandingPattern = 7, LandingRowSpacing = 8,
                PlantHeight = 9, MaxPlantHeight = 10, PlantColor = " ", Maturity = null, YieldKg = 0,
                MinFruitWeight = 11, MaxFruitWeight = 12, MinFruitSize = 0, MaxFruitSize = 15,
                Plants = [new() { PlantCulture = new() { Name = "Культура", Class = "Мой класс" } }] };
            db.PlantsSort.Add(sort); await db.SaveChangesAsync(); return sort.Id;
        });
        var service = _fixture.CreateLibraryService();
        var read = (await service.GetSortAsync(id)).ValueOrThrow(); var values = read.Characteristics;
        Assert.Equal("  Сорт  ", read.Name); Assert.Equal("", values.Description.ToNullable());
        Assert.Equal(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new[] { values.MinGerminationTime.ToNullable(),
            values.MaxGerminationTime.ToNullable(), values.AgeOfSeedlings.ToNullable(), values.MaxAgeOfSeedlings.ToNullable(),
            values.GrowingSeason.ToNullable(), values.MaxGrowingSeason.ToNullable(), values.LandingPattern.ToNullable(),
            values.LandingRowSpacing.ToNullable(), values.PlantHeight.ToNullable(), values.MaxPlantHeight.ToNullable() });
        Assert.Equal(" ", values.PlantColor.ToNullable()); Assert.IsType<Option<string>.None>(values.Maturity);
        Assert.Equal(0, values.MinFruitSize.ToNullable()); Assert.Equal(15, values.MaxFruitSize.ToNullable());
        Assert.Equal(0, values.YieldKg.ToNullable()); Assert.Equal(11, values.MinFruitWeight.ToNullable()); Assert.Equal(12, values.MaxFruitWeight.ToNullable());
        Assert.Equal("Фирма", (await service.GetProducerAsync(read.Producer.Id)).ValueOrThrow().Name);
        Assert.Equal("Мой класс", (await service.GetCultureAsync(read.CultureId.Match(v => v, () => throw new Exception()))).ValueOrThrow().Class);
        // Снимок не разделяет EF-граф после закрытия контекста.
        await _fixture.ExecuteAsync(async db => { var sort = await db.PlantsSort.FindAsync(id); sort!.Name = "Изменено"; await db.SaveChangesAsync(); });
        Assert.Equal("  Сорт  ", read.Name);
    }

    [Fact]
    public async Task Read_UnlinkedSort_ReturnsExplicitAbsentCulture()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var sort = new PlantSort { Name = "Сорт", Producer = new() { Name = "Фирма" } };
            db.PlantsSort.Add(sort); await db.SaveChangesAsync(); return sort.Id;
        });
        Assert.IsType<Option<int>.None>((await _fixture.CreateLibraryService().GetSortAsync(id)).ValueOrThrow().CultureId);
    }
}
