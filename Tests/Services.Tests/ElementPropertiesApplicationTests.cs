using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class ElementPropertiesApplicationTests
{
    private static ElementEditorValues Values() => new(new("", Option.None<string>(), Option.None<string>(), Option.None<string>(), Option.None<string>()), 0, 0, 90, 90);

    [Theory]
    [InlineData(0, 90)]
    [InlineData(1, -1)]
    [InlineData(1, double.NaN)]
    public async Task Execute_InvalidInput_DoesNotCallWriter(int id, double width)
    {
        var writer = Substitute.For<IElementPropertiesWriter>();
        var result = await new EditElementProperties(writer).ExecuteAsync(new(id, Values(), Values() with { Width = width }, 150));
        Assert.True(result.IsFailure);
        await writer.DidNotReceive().ApplyAsync(Arg.Any<ElementPropertiesEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ValidRequest_ForwardsTokenAndFailure()
    {
        var writer = Substitute.For<IElementPropertiesWriter>();
        using var source = new CancellationTokenSource();
        writer.ApplyAsync(Arg.Any<ElementPropertiesEdit>(), source.Token).Returns(Result.Failure<ElementPropertiesResultFromViewModel>("Отказ"));
        var result = await new EditElementProperties(writer).ExecuteAsync(new(1, Values(), Values() with { X = 30 }, 150), source.Token);
        Assert.Equal("Отказ", result.Match(_ => "", error => error));
        await writer.Received(1).ApplyAsync(Arg.Is<ElementPropertiesEdit>(edit => edit.HasGeometryChanges && edit.Id == 1), source.Token);
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotCallWriter()
    {
        var writer = Substitute.For<IElementPropertiesWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EditElementProperties(writer).ExecuteAsync(new(1, Values(), Values(), 150), source.Token));
        await writer.DidNotReceive().ApplyAsync(Arg.Any<ElementPropertiesEdit>(), Arg.Any<CancellationToken>());
    }
}
