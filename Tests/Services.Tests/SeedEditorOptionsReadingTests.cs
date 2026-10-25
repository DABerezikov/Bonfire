using Bonfire.Domain.Functional;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedEditorOptionsReadingTests
{
    [Fact]
    public async Task Read_ProjectsThreeQueriesWithoutGraphs_PreservesRawValuesInStorage()
    {
        var commands = new List<string>(); using var fixture = new SqliteUowFixture(commands.Add);
        await fixture.ExecuteAsync(async db =>
        {
            db.AddRange(new PlantSort { Name = "Сорт", PlantColor = " Красный ", Maturity = "Ранний", Producer = new() { Name = "Фирма" } },
                new SortOption { Category = "Цвет", Name = "красный" }, new SortOption { Category = "Цвет", Name = "Белый" },
                new SortOption { Category = "Созревание", Name = " Поздний " }, new SortOption { Category = "Иное", Name = "Игнорировать" });
            await db.SaveChangesAsync();
        });
        commands.Clear();
        var result = (await fixture.CreateSeedsService().GetEditorOptionsAsync()).ValueOrThrow();
        Assert.Equal(3, commands.Count);
        Assert.All(commands, sql => Assert.DoesNotContain("JOIN", sql, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Белый", "Красный" }, result.Colors);
        Assert.Contains("Поздний", result.Maturities); Assert.Contains("Ранний", result.Maturities);
        Assert.Equal("Фирма", Assert.Single(result.Producers).Name);
        Assert.Equal(" Красный ", await fixture.QueryAsync(db => db.PlantsSort.Select(s => s.PlantColor).SingleAsync()));
    }

    [Fact]
    public async Task Read_DatabaseRefuses_ReturnsDiagnostic()
    {
        using var fixture = new SqliteUowFixture();
        await fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("DROP TABLE SortOptions;"));
        var result = await fixture.CreateSeedsService().GetEditorOptionsAsync();
        Assert.True(result.IsFailure); Assert.Contains("варианты редактора", result.Match(_ => "не ожидалось", error => error));
    }
}
