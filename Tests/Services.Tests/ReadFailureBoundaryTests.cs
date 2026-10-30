using Bonfire.Domain.Functional;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class ReadFailureBoundaryTests
{
    [Theory]
    [InlineData("Seeds", typeof(Seed))]
    [InlineData("Seedlings", typeof(Seedling))]
    [InlineData("Seedling", typeof(Seedling))]
    [InlineData("Plans", typeof(GardenPlan))]
    [InlineData("Gardens", typeof(Garden))]
    [InlineData("Sort", typeof(PlantSort))]
    [InlineData("Culture", typeof(PlantCulture))]
    [InlineData("Producer", typeof(Producer))]
    public async Task Read_DatabaseUnavailable_ReturnsDiagnosticWithoutThrowing(string kind, Type entity)
    {
        using var fixture = new SqliteUowFixture();
        var table = await fixture.QueryAsync(db => Task.FromResult(db.Model.FindEntityType(entity)!.GetTableName()!));
        Assert.Matches("^[A-Za-z_][A-Za-z0-9_]*$", table);
        var command = "DROP TABLE \"" + table + "\";";
        await fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(command));
        var error = kind switch
        {
            "Seeds" => (await fixture.CreateSeedsService().ReadSeedsAsync()).Match(_ => "", failure => failure),
            "Seedlings" => (await fixture.CreateSeedlingsService().ReadSeedlingsAsync()).Match(_ => "", failure => failure),
            "Seedling" => (await fixture.CreateSeedlingsService().ReadSeedlingAsync(1)).Match(_ => "", failure => failure),
            "Plans" => (await fixture.CreateGardenService().ReadPlansAsync()).Match(_ => "", failure => failure),
            "Gardens" => (await fixture.CreateGardenService().ReadGardensAsync(1)).Match(_ => "", failure => failure),
            "Sort" => (await fixture.CreateLibraryService().GetSortAsync(1)).Match(_ => "", failure => failure),
            "Culture" => (await fixture.CreateLibraryService().GetCultureAsync(1)).Match(_ => "", failure => failure),
            _ => (await fixture.CreateLibraryService().GetProducerAsync(1)).Match(_ => "", failure => failure)
        };
        Assert.Contains("Ошибка чтения", error); Assert.Contains(table, error);
    }

    [Fact]
    public async Task Read_Cancellation_PropagatesCancellationRatherThanStorageFailure()
    {
        using var fixture = new SqliteUowFixture(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new BonfireDB.SeedEditing.DbSeedReader(fixture.Factory).ReadAsync(cancellation.Token));
    }
}
