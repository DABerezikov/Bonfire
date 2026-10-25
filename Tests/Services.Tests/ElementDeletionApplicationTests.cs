using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class ElementDeletionApplicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        var writer = Substitute.For<IElementDeletionWriter>();
        Assert.True((await new DeleteGardenElement(writer).ExecuteAsync(new(id))).IsFailure);
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenElementId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedId_ForwardsTokenAndReceipt()
    {
        var writer = Substitute.For<IElementDeletionWriter>();
        using var source = new CancellationTokenSource();
        writer.DeleteAsync(Arg.Any<GardenElementId>(), source.Token).Returns(Result.Success(new ElementDeletionResultFromViewModel(7)));
        Assert.Equal(7, (await new DeleteGardenElement(writer).ExecuteAsync(new(7), source.Token)).ValueOrThrow().ElementId);
        await writer.Received(1).DeleteAsync(Arg.Is<GardenElementId>(id => id.Value == 7), source.Token);
    }

    [Fact]
    public async Task Execute_WriterFailure_IsPreserved()
    {
        var writer = Substitute.For<IElementDeletionWriter>();
        writer.DeleteAsync(Arg.Any<GardenElementId>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<ElementDeletionResultFromViewModel>("Отказ удаления"));
        var result = await new DeleteGardenElement(writer).ExecuteAsync(new(1));
        Assert.Equal("Отказ удаления", result.Match(_ => "", error => error));
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        var writer = Substitute.For<IElementDeletionWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteGardenElement(writer).ExecuteAsync(new(1), source.Token));
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenElementId>(), Arg.Any<CancellationToken>());
    }
}
