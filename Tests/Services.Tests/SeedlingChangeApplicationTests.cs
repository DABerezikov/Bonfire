using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Services.Tests;

public class SeedlingChangeApplicationTests
{
    private readonly ISeedlingChangeWriter _writer = Substitute.For<ISeedlingChangeWriter>();

    [Fact]
    public async Task Execute_ValidRequest_ForwardsIntentAndToken()
    {
        var action = new SeedlingAction.Germinate(3, DateTime.Today);
        var saved = new SeedlingChangeResultFromViewModel(7, []);
        _writer.ApplyAsync(Arg.Any<SeedlingId>(), action, Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        Assert.Equal(saved, (await new ChangeSeedling(_writer).ExecuteAsync(new(7, action), cancellation.Token)).ValueOrThrow());
        await _writer.Received(1).ApplyAsync(Arg.Is<SeedlingId>(id => id.Value == 7), action, cancellation.Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        Assert.True((await new ChangeSeedling(_writer).ExecuteAsync(new(id, new SeedlingAction.Germinate(1, DateTime.Today)))).IsFailure);
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<SeedlingId>(), Arg.Any<SeedlingAction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ChangeSeedling(_writer).ExecuteAsync(new(7, new SeedlingAction.Germinate(1, DateTime.Today)), cancellation.Token));
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<SeedlingId>(), Arg.Any<SeedlingAction>(), Arg.Any<CancellationToken>());
    }
}
