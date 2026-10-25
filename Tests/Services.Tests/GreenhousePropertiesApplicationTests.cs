using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GreenhousePropertiesApplicationTests
{
    private static GreenhouseEditorValues Values() => new("", Option.None<string>(), Option.None<string>(), 6, 3);
    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, -1)]
    [InlineData(1, double.NaN)]
    [InlineData(1, double.PositiveInfinity)]
    public async Task Execute_InvalidInput_DoesNotCallWriter(int id, double width)
    {
        var writer = Substitute.For<IGreenhousePropertiesWriter>();
        Assert.True((await new EditGreenhouseProperties(writer).ExecuteAsync(new(id, Values(), Values() with { WidthMeters = width }, 150))).IsFailure);
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GreenhousePropertiesEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedEdit_ForwardsTokenAndWriterFailure()
    {
        var writer = Substitute.For<IGreenhousePropertiesWriter>();
        using var source = new CancellationTokenSource();
        writer.ApplyAsync(Arg.Any<GreenhousePropertiesEdit>(), source.Token).Returns(Result.Failure<GreenhousePropertiesResultFromViewModel>("Отказ БД"));
        var result = await new EditGreenhouseProperties(writer).ExecuteAsync(new(1, Values(), Values() with { WidthMeters = 5 }, 150), source.Token);
        Assert.Equal("Отказ БД", result.Match(_ => "", error => error));
        await writer.Received(1).ApplyAsync(Arg.Is<GreenhousePropertiesEdit>(edit => edit.HasSizeChanges && edit.Id == 1), source.Token);
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotCallWriter()
    {
        var writer = Substitute.For<IGreenhousePropertiesWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EditGreenhouseProperties(writer).ExecuteAsync(new(1, Values(), Values(), 150), source.Token));
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GreenhousePropertiesEdit>(), Arg.Any<CancellationToken>());
    }
}
