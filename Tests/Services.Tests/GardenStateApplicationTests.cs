using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;

namespace Services.Tests;

public class GardenStateApplicationTests
{
    private readonly IGardenStateWriter _writer = Substitute.For<IGardenStateWriter>();

    [Fact]
    public async Task Execute_ValidRequest_ForwardsIntentAndToken()
    {
        var target = new StateChangeTarget.Element(new PreparedState());
        GardenStateResultFromViewModel saved = new GardenStateResultFromViewModel.Element(7, target.State);
        _writer.ApplyAsync(Arg.Any<GardenStateChange>(), Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        Assert.Same(saved, (await new ChangeGardenState(_writer).ExecuteAsync(new(7, target), cancellation.Token)).ValueOrThrow());
        await _writer.Received(1).ApplyAsync(Arg.Is<GardenStateChange>(change => change.EntityId == 7 && change.Target == target), cancellation.Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        Assert.True((await new ChangeGardenState(_writer).ExecuteAsync(new(id, new StateChangeTarget.Greenhouse(new PreparedState())))).IsFailure);
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<GardenStateChange>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ChangeGardenState(_writer).ExecuteAsync(new(7, new StateChangeTarget.Spot(new SpotChange.Clear())), cancellation.Token));
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<GardenStateChange>(), Arg.Any<CancellationToken>());
    }
}
