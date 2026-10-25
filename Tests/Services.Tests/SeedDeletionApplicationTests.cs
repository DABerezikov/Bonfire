using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class SeedDeletionApplicationTests
{
    private readonly ISeedDeletionWriter _writer = Substitute.For<ISeedDeletionWriter>();

    [Fact]
    public async Task Execute_ValidInput_PassesNormalizedReasonAndCancellation()
    {
        _writer.DeleteAsync(Arg.Any<SeedDeletion>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SeedDeletionResultFromViewModel(1)));
        using var cancellation = new CancellationTokenSource();
        var result = await new DeleteSeed(_writer).ExecuteAsync(new(1, " Причина "), cancellation.Token);
        Assert.Equal(1, result.ValueOrThrow().SeedId);
        await _writer.Received(1).DeleteAsync(Arg.Is<SeedDeletion>(deletion => deletion.SeedId.Value == 1 && deletion.Reason == "Причина"), cancellation.Token);
    }

    [Theory]
    [InlineData(0, "Причина")]
    [InlineData(-1, "Причина")]
    [InlineData(1, null)]
    public async Task Execute_InvalidInput_DoesNotWrite(int id, string? reason)
    {
        Assert.True((await new DeleteSeed(_writer).ExecuteAsync(new(id, reason!))).IsFailure);
        await _writer.DidNotReceive().DeleteAsync(Arg.Any<SeedDeletion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.DeleteAsync(Arg.Any<SeedDeletion>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<SeedDeletionResultFromViewModel>("Запись семян удалена"));
        var result = await new DeleteSeed(_writer).ExecuteAsync(new(1, ""));
        Assert.Equal("Запись семян удалена", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_PreCancelledToken_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteSeed(_writer).ExecuteAsync(new(1, ""), cancellation.Token));
        await _writer.DidNotReceive().DeleteAsync(Arg.Any<SeedDeletion>(), Arg.Any<CancellationToken>());
    }
}
