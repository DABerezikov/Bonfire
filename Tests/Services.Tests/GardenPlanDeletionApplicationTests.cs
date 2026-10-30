using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GardenPlanDeletionApplicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        var writer = Substitute.For<IGardenPlanDeletionWriter>();
        Assert.True((await new DeleteGardenPlan(writer).ExecuteAsync(new(id))).IsFailure);
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenPlanId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedId_ForwardsTokenAndReceipt()
    {
        var writer = Substitute.For<IGardenPlanDeletionWriter>();
        using var source = new CancellationTokenSource();
        writer.DeleteAsync(Arg.Any<GardenPlanId>(), source.Token).Returns(Result.Success(new GardenPlanDeletionResultFromViewModel(7)));
        Assert.Equal(7, (await new DeleteGardenPlan(writer).ExecuteAsync(new(7), source.Token)).ValueOrThrow().GardenPlanId);
        await writer.Received(1).DeleteAsync(Arg.Is<GardenPlanId>(id => id.Value == 7), source.Token);
    }

    [Fact]
    public async Task Execute_WriterFailure_IsPreserved()
    {
        var writer = Substitute.For<IGardenPlanDeletionWriter>();
        writer.DeleteAsync(Arg.Any<GardenPlanId>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<GardenPlanDeletionResultFromViewModel>("Отказ удаления"));
        var result = await new DeleteGardenPlan(writer).ExecuteAsync(new(1));
        Assert.Equal("Отказ удаления", result.Match(_ => "", error => error));
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        var writer = Substitute.For<IGardenPlanDeletionWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteGardenPlan(writer).ExecuteAsync(new(1), source.Token));
        await writer.DidNotReceive().DeleteAsync(Arg.Any<GardenPlanId>(), Arg.Any<CancellationToken>());
    }
}
