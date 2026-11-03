using Bonfire.Application.Caching;
using Bonfire.Domain.Functional;

namespace Services.Tests;

public class ReadModelCacheTests
{
    [Fact]
    public async Task SameSnapshotIsReadOnce_AndOnlyAffectedAreaIsInvalidated()
    {
        var cache = new ReadModelCache(); var reads = 0;
        Task<Result<int>> Read(CancellationToken _) => Task.FromResult(Result.Success(++reads));
        Assert.Equal(1, (await cache.ReadAsync(ReadArea.Seeds, "all", Read)).ValueOrThrow());
        Assert.Equal(1, (await cache.ReadAsync(ReadArea.Seeds, "all", Read)).ValueOrThrow());
        cache.Invalidate(ReadArea.Harvest);
        Assert.Equal(1, (await cache.ReadAsync(ReadArea.Seeds, "all", Read)).ValueOrThrow());
        cache.Invalidate(ReadArea.Seeds);
        Assert.Equal(2, (await cache.ReadAsync(ReadArea.Seeds, "all", Read)).ValueOrThrow());
        Assert.True(cache.Version(ReadArea.Seeds) > 0);
    }

    [Fact]
    public async Task ParallelReadsShareWork_AndCancelledWaiterDoesNotCancelOthers()
    {
        var cache = new ReadModelCache(); var pending = new TaskCompletionSource<Result<int>>(); var calls = 0;
        Task<Result<int>> Read(CancellationToken token) { Assert.False(token.CanBeCanceled); calls++; return pending.Task; }
        using var cancelled = new CancellationTokenSource();
        var first = cache.ReadAsync(ReadArea.Seeds, "all", Read, cancelled.Token);
        var second = cache.ReadAsync(ReadArea.Seeds, "all", Read);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first.WaitAsync(TimeSpan.FromSeconds(5)));
        pending.SetResult(Result.Success(7));
        Assert.Equal(1, calls);
        Assert.Equal(7, (await second).ValueOrThrow());
    }

    [Fact]
    public async Task InvalidationDuringReadDoesNotReturnOrCacheOldSnapshot()
    {
        var cache = new ReadModelCache(); var pending = new TaskCompletionSource<Result<int>>(); var calls = 0;
        Task<Result<int>> Read(CancellationToken _) => ++calls == 1 ? pending.Task : Task.FromResult(Result.Success(2));
        var result = cache.ReadAsync(ReadArea.Seeds, "all", Read);
        cache.Invalidate(ReadArea.Seeds); pending.SetResult(Result.Success(1));
        Assert.Equal(2, (await result).ValueOrThrow());
        Assert.Equal(2, (await cache.ReadAsync(ReadArea.Seeds, "all", Read)).ValueOrThrow());
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task FailureIsReturned_AndNextReadRetries()
    {
        var cache = new ReadModelCache(); var calls = 0;
        Task<Result<int>> Read(CancellationToken _) => Task.FromResult(++calls == 1 ? Result.Failure<int>("Ошибка чтения") : Result.Success(2));
        Assert.True((await cache.ReadAsync(ReadArea.Catalog, "all", Read)).IsFailure);
        Assert.Equal(2, (await cache.ReadAsync(ReadArea.Catalog, "all", Read)).ValueOrThrow());
    }
}
