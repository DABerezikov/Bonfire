using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GardenDeletionApplicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        var writer = Substitute.For<IGardenDeletionWriter>();
        Assert.True((await new DeleteGarden(writer).ExecuteAsync(new(id))).IsFailure);
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedId_ForwardsTokenAndReceipt()
    {
        var writer = Substitute.For<IGardenDeletionWriter>();
        using var source = new CancellationTokenSource();
        writer.DeleteAsync(Arg.Any<GardenId>(), source.Token).Returns(Result.Success(new GardenDeletionResultFromViewModel(7)));
        Assert.Equal(7, (await new DeleteGarden(writer).ExecuteAsync(new(7), source.Token)).ValueOrThrow().GardenId);
        await writer.Received(1).DeleteAsync(Arg.Is<GardenId>(id => id.Value == 7), source.Token);
    }

    [Fact]
    public async Task Execute_WriterFailure_IsPreserved()
    {
        var writer = Substitute.For<IGardenDeletionWriter>();
        writer.DeleteAsync(Arg.Any<GardenId>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<GardenDeletionResultFromViewModel>("Отказ удаления"));
        var result = await new DeleteGarden(writer).ExecuteAsync(new(1));
        Assert.Equal("Отказ удаления", result.Match(_ => "", error => error));
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        var writer = Substitute.For<IGardenDeletionWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteGarden(writer).ExecuteAsync(new(1), source.Token));
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenId>(), Arg.Any<CancellationToken>());
    }
}
