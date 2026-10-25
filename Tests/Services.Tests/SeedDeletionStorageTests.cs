using Bonfire.Domain.Seeds;
using BonfireDB.SeedEditing;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedDeletionStorageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_DatabaseFailure_ReturnsReasonInsteadOfThrowing(bool concurrency)
    {
        var unit = Substitute.For<IUnitOfWork>();
        var seeds = Substitute.For<IRepository<Seed>>();
        unit.Repository<Seed>().Returns(seeds);
        seeds.GetAsync(1).Returns(new Seed { Id = 1, SeedsInfo = new() });
        Exception failure = concurrency ? new DbUpdateConcurrencyException()
            : new DbUpdateException("DB", new Exception("Причина отказа"));
        unit.SaveChangesAsync().Returns<int>(_ => throw failure);
        var result = await new DbSeedDeletionWriter(unit.ToFactory()).DeleteAsync(
            SeedDeletion.Create(1, "Причина").ValueOrThrow(), CancellationToken.None);
        Assert.Contains(concurrency ? "другим процессом" : "Причина отказа", result.Match(_ => "не ожидалось", error => error));
    }
}
