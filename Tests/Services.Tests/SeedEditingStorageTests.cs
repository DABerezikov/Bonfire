using Bonfire.Application.Seeds;
using Bonfire.Domain.Seeds;
using BonfireDB.SeedEditing;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedEditingStorageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_ExpectedDatabaseFailure_ReturnsReasonInsteadOfThrowing(bool concurrency)
    {
        var unit = Substitute.For<IUnitOfWork>();
        var seeds = Substitute.For<IRepository<Seed>>();
        var producers = Substitute.For<IRepository<Producer>>();
        unit.Repository<Seed>().Returns(seeds);
        unit.Repository<Producer>().Returns(producers);
        seeds.GetAsync(1).Returns(new Seed
        {
            Id = 1, SeedsInfo = new(), Plant = new()
                { PlantSort = new() { Id = 2, Name = "Сорт", Producer = new() } }
        });
        producers.GetAsync(3).Returns(new Producer { Id = 3, Name = "Фирма" });
        Exception failure = concurrency ? new DbUpdateConcurrencyException()
            : new DbUpdateException("DB", new Exception("Причина отказа"));
        unit.SaveChangesAsync().Returns<int>(_ => throw failure);
        var edit = SeedEdit.Create(1, 2, 3, "Новое", new(), new()).ValueOrThrow();
        var result = await new DbSeedEditWriter(unit.ToFactory()).SaveAsync(edit, CancellationToken.None);
        Assert.Contains(concurrency ? "другим процессом" : "Причина отказа", result.Match(_ => "не ожидалось", error => error));
    }
}
