using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Services.Tests;

public class ProducerApplicationTests
{
    private readonly IProducerWriter _writer = Substitute.For<IProducerWriter>();

    [Fact]
    public async Task Execute_ValidName_SendsValidatedProducerAndReturnsSavedValue()
    {
        _writer.SaveAsync(Arg.Any<ProducerDefinition>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ProducerDetailsFromViewModel(1, "Новое имя")));
        var result = await new UpdateProducer(_writer).ExecuteAsync(new(1, "Новое имя"));
        Assert.Equal("Новое имя", result.ValueOrThrow().Name);
        await _writer.Received(1).SaveAsync(Arg.Is<ProducerDefinition>(producer =>
            producer.Id.Value == 1 && producer.Name.Value == "Новое имя"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "Фирма")]
    [InlineData(1, "")]
    [InlineData(1, " ")]
    public async Task Execute_InvalidInput_DoesNotWrite(int id, string name)
    {
        Assert.True((await new UpdateProducer(_writer).ExecuteAsync(new(id, name))).IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<ProducerDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.SaveAsync(Arg.Any<ProducerDefinition>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProducerDetailsFromViewModel>("Производитель удалён"));
        var result = await new UpdateProducer(_writer).ExecuteAsync(new(1, "Фирма"));
        Assert.Equal("Производитель удалён", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_PreCancelledToken_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UpdateProducer(_writer).ExecuteAsync(new(1, "Фирма"), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<ProducerDefinition>(), Arg.Any<CancellationToken>());
    }
}
