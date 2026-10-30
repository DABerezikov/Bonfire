using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GreenhouseDeletionApplicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        var writer = Substitute.For<IGreenhouseDeletionWriter>();
        Assert.True((await new DeleteGreenhouse(writer).ExecuteAsync(new(id))).IsFailure);
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GreenhouseId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedId_ForwardsTokenAndReceipt()
    {
        var writer = Substitute.For<IGreenhouseDeletionWriter>();
        using var source = new CancellationTokenSource();
        writer.DeleteAsync(Arg.Any<GreenhouseId>(), source.Token).Returns(Result.Success(new GreenhouseDeletionResultFromViewModel(7)));
        Assert.Equal(7, (await new DeleteGreenhouse(writer).ExecuteAsync(new(7), source.Token)).ValueOrThrow().GreenhouseId);
        await writer.Received(1).DeleteAsync(Arg.Is<GreenhouseId>(id => id.Value == 7), source.Token);
    }

    [Fact]
    public async Task Execute_WriterFailure_IsPreserved()
    {
        var writer = Substitute.For<IGreenhouseDeletionWriter>();
        writer.DeleteAsync(Arg.Any<GreenhouseId>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<GreenhouseDeletionResultFromViewModel>("Отказ удаления"));
        var result = await new DeleteGreenhouse(writer).ExecuteAsync(new(1));
        Assert.Equal("Отказ удаления", result.Match(_ => "", error => error));
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        var writer = Substitute.For<IGreenhouseDeletionWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteGreenhouse(writer).ExecuteAsync(new(1), source.Token));
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GreenhouseId>(), Arg.Any<CancellationToken>());
    }
}
