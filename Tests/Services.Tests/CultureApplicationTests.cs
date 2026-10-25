using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Services.Tests;

public class CultureApplicationTests
{
    private readonly ICultureWriter _writer = Substitute.For<ICultureWriter>();

    [Fact]
    public async Task Execute_ValidDetails_PassesValidatedCultureAndReturnsStoredValues()
    {
        _writer.SaveAsync(Arg.Any<CultureDefinition>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CultureDetailsFromViewModel(1, "Астра", "Цветы")));
        var result = await new UpdateCulture(_writer).ExecuteAsync(new(1, "Астра", "Цветы"));
        Assert.Equal("Астра", result.ValueOrThrow().Name);
        await _writer.Received(1).SaveAsync(Arg.Is<CultureDefinition>(culture =>
            culture.Id.Value == 1 && culture.Name == "Астра" && culture.Class == "Цветы"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "Томат", "Овощи")]
    [InlineData(1, "", "Овощи")]
    [InlineData(1, "Томат", "")]
    public async Task Execute_InvalidDetails_DoesNotWrite(int id, string name, string plantClass)
    {
        var result = await new UpdateCulture(_writer).ExecuteAsync(new(id, name, plantClass));
        Assert.True(result.IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<CultureDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.SaveAsync(Arg.Any<CultureDefinition>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CultureDetailsFromViewModel>("Культура удалена"));
        var result = await new UpdateCulture(_writer).ExecuteAsync(new(1, "Томат", "Овощи"));
        Assert.Equal("Культура удалена", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_CancelledBeforeStart_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UpdateCulture(_writer).ExecuteAsync(new(1, "Томат", "Овощи"), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<CultureDefinition>(), Arg.Any<CancellationToken>());
    }
}
