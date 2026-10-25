using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Services.Tests;

public class GridResizingApplicationTests
{
    private readonly IGridWriter _writer = Substitute.For<IGridWriter>();

    [Fact]
    public async Task Execute_ValidRequest_ForwardsSizeTokenAndSavedResult()
    {
        var saved = new GridResultFromViewModel(7, 2, 3, []);
        _writer.ApplyAsync(7, Arg.Any<GridSize>(), Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        Assert.Same(saved, (await new ResizeGrid(_writer).ExecuteAsync(new(7, 2, 3), cancellation.Token)).ValueOrThrow());
        await _writer.Received(1).ApplyAsync(7, Arg.Is<GridSize>(size => size.Rows == 2 && size.Columns == 3), cancellation.Token);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(7, -1, 1)]
    [InlineData(7, int.MaxValue, 2)]
    public async Task Execute_InvalidRequest_DoesNotWrite(int id, int rows, int columns)
    {
        Assert.True((await new ResizeGrid(_writer).ExecuteAsync(new(id, rows, columns))).IsFailure);
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<int>(), Arg.Any<GridSize>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ResizeGrid(_writer).ExecuteAsync(new(7, 1, 1), cancellation.Token));
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<int>(), Arg.Any<GridSize>(), Arg.Any<CancellationToken>());
    }
}
