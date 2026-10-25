using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Services.Tests;

public class SortApplicationTests
{
    private readonly ISortWriter _writer = Substitute.For<ISortWriter>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_ValidEdit_PassesValidatedIntentAndReturnsSavedLinks(bool changeCulture)
    {
        var saved = new SortDetailsFromViewModel(1, "Имя", [new(4, new(2, "Томат", "Овощи"))]);
        _writer.SaveAsync(Arg.Any<SortLibraryEdit>(), Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        var result = await new UpdateSort(_writer).ExecuteAsync(new(1, " Имя ",
            changeCulture ? Option.Some(2) : Option.None<int>()), cancellation.Token);
        Assert.Same(saved, result.ValueOrThrow());
        await _writer.Received(1).SaveAsync(Arg.Is<SortLibraryEdit>(edit => edit.Id.Value == 1
            && edit.Name.Value == "Имя" && edit.CultureChange.Match(c => changeCulture && c.Value == 2, () => !changeCulture)), cancellation.Token);
    }

    [Theory]
    [InlineData(0, "Имя", 2)]
    [InlineData(1, "", 2)]
    [InlineData(1, "Имя", 0)]
    public async Task Execute_InvalidInput_DoesNotWrite(int id, string name, int culture)
    {
        Assert.True((await new UpdateSort(_writer).ExecuteAsync(new(id, name, Option.Some(culture)))).IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SortLibraryEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.SaveAsync(Arg.Any<SortLibraryEdit>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SortDetailsFromViewModel>("Сорт удалён"));
        var result = await new UpdateSort(_writer).ExecuteAsync(new(1, "Имя", Option.None<int>()));
        Assert.Equal("Сорт удалён", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_PreCancelledToken_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UpdateSort(_writer)
            .ExecuteAsync(new(1, "Имя", Option.None<int>()), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SortLibraryEdit>(), Arg.Any<CancellationToken>());
    }
}
