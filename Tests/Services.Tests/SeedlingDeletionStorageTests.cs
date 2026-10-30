using Bonfire.Domain.Seedlings;
using BonfireDB.Sowing;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedlingDeletionStorageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_SaveFailure_ReturnsReasonAndDoesNotReadSeedsForDiscard(bool concurrency)
    {
        var unit = Substitute.For<IUnitOfWork>();
        var batches = Substitute.For<IRepository<Seedling>>();
        unit.Repository<Seedling>().Returns(batches);
        batches.GetAsync(7).Returns(new Seedling { Id = 7 });
        Exception failure = concurrency ? new DbUpdateConcurrencyException()
            : new DbUpdateException("DB", new Exception("Причина отказа"));
        unit.SaveChangesAsync().Returns<int>(_ => throw failure);
        var result = await new DbSeedlingDeletionWriter(unit.ToFactory()).DeleteAsync(
            SeedlingId.Create(7).ValueOrThrow(), new SeedlingRemoval.Discard(), CancellationToken.None);
        Assert.Contains(concurrency ? "другим процессом" : "Причина отказа", result.Match(_ => "не ожидалось", error => error));
        unit.DidNotReceive().Repository<Seed>();
    }
}
