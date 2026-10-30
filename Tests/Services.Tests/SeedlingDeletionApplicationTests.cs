using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Services.Tests;

public class SeedlingDeletionApplicationTests
{
    private readonly ISeedlingDeletionWriter _writer = Substitute.For<ISeedlingDeletionWriter>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_ValidRequest_PreservesChoiceAndCancellation(bool returnSeeds)
    {
        SeedlingRemoval choice = returnSeeds ? new SeedlingRemoval.CorrectSowing() : new SeedlingRemoval.Discard();
        var saved = new SeedlingDeletionResultFromViewModel(7, Option.None<ReturnedSeedStockFromViewModel>());
        _writer.DeleteAsync(Arg.Any<SeedlingId>(), choice, Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        var result = await new DeleteSeedling(_writer).ExecuteAsync(new(7, choice), cancellation.Token);
        Assert.Equal(saved, result.ValueOrThrow());
        await _writer.Received(1).DeleteAsync(Arg.Is<SeedlingId>(id => id.Value == 7), choice, cancellation.Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Execute_InvalidId_DoesNotWrite(int id)
    {
        Assert.True((await new DeleteSeedling(_writer).ExecuteAsync(new(id, new SeedlingRemoval.Discard()))).IsFailure);
        await _writer.DidNotReceive().DeleteAsync(Arg.Any<SeedlingId>(), Arg.Any<SeedlingRemoval>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DeleteSeedling(_writer).ExecuteAsync(new(1, new SeedlingRemoval.Discard()), cancellation.Token));
        await _writer.DidNotReceive().DeleteAsync(Arg.Any<SeedlingId>(), Arg.Any<SeedlingRemoval>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesMessage()
    {
        _writer.DeleteAsync(Arg.Any<SeedlingId>(), Arg.Any<SeedlingRemoval>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<SeedlingDeletionResultFromViewModel>("Пакет удалён"));
        var result = await new DeleteSeedling(_writer).ExecuteAsync(new(1, new SeedlingRemoval.CorrectSowing()));
        Assert.Equal("Пакет удалён", result.Match(_ => "не ожидалось", error => error));
    }
}
