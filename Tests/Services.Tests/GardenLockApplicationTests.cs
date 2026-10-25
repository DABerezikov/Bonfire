using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GardenLockApplicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        var writer = Substitute.For<IGardenLockWriter>();
        Assert.True((await new SetGardenLock(writer).ExecuteAsync(new(id, new GardenLockTarget.Element(), true))).IsFailure);
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GardenLockChange>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Execute_ExplicitState_ForwardsCheckedChangeAndToken(bool greenhouse, bool locked)
    {
        var writer = Substitute.For<IGardenLockWriter>();
        using var source = new CancellationTokenSource();
        GardenLockTarget target = greenhouse ? new GardenLockTarget.Greenhouse() : new GardenLockTarget.Element();
        writer.ApplyAsync(Arg.Any<GardenLockChange>(), source.Token).Returns(Result.Success(new GardenLockResultFromViewModel(7, locked)));
        var saved = (await new SetGardenLock(writer).ExecuteAsync(new(7, target, locked), source.Token)).ValueOrThrow();
        Assert.Equal((7, locked), (saved.Id, saved.IsLocked));
        await writer.Received(1).ApplyAsync(Arg.Is<GardenLockChange>(change => change.Id == 7 && change.Target == target && change.IsLocked == locked), source.Token);
    }

    [Fact]
    public async Task Execute_WriterFailure_IsPreserved()
    {
        var writer = Substitute.For<IGardenLockWriter>();
        writer.ApplyAsync(Arg.Any<GardenLockChange>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<GardenLockResultFromViewModel>("Отказ БД"));
        var result = await new SetGardenLock(writer).ExecuteAsync(new(1, new GardenLockTarget.Greenhouse(), false));
        Assert.Equal("Отказ БД", result.Match(_ => "", error => error));
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        var writer = Substitute.For<IGardenLockWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SetGardenLock(writer).ExecuteAsync(new(1, new GardenLockTarget.Element(), true), source.Token));
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GardenLockChange>(), Arg.Any<CancellationToken>());
    }
}
