using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GardenGeometryApplicationTests
{
    [Theory]
    [InlineData(0, 90)]
    [InlineData(1, -90)]
    [InlineData(1, double.NaN)]
    public async Task Execute_InvalidRequest_DoesNotCallWriter(int id, double width)
    {
        var writer = Substitute.For<IGardenGeometryWriter>();
        var result = await new SaveGardenGeometry(writer).ExecuteAsync(new(id, new GeometryTarget.Greenhouse(), 0, 0, width, 90));
        Assert.True(result.IsFailure);
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GardenGeometryEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_CheckedEdit_ForwardsTokenAndWriterFailure()
    {
        var writer = Substitute.For<IGardenGeometryWriter>();
        using var source = new CancellationTokenSource();
        writer.ApplyAsync(Arg.Any<GardenGeometryEdit>(), source.Token).Returns(Result.Failure<GeometryResultFromViewModel>("БД недоступна"));
        var result = await new SaveGardenGeometry(writer).ExecuteAsync(new(1, new GeometryTarget.Element(150), 30, 45, 180, 90), source.Token);
        Assert.Equal("БД недоступна", result.Match(_ => "", error => error));
        await writer.Received(1).ApplyAsync(Arg.Is<GardenGeometryEdit>(edit => edit is GardenGeometryEdit.Element && edit.Id == 1 && edit.Rectangle.X == 30), source.Token);
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotCallWriter()
    {
        var writer = Substitute.For<IGardenGeometryWriter>();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SaveGardenGeometry(writer).ExecuteAsync(new(1, new GeometryTarget.Greenhouse(), 0, 0, 90, 90), source.Token));
        await writer.DidNotReceive().ApplyAsync(Arg.Any<GardenGeometryEdit>(), Arg.Any<CancellationToken>());
    }
}
