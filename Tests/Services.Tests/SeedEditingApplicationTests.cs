using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class SeedEditingApplicationTests
{
    private readonly ISeedEditWriter _writer = Substitute.For<ISeedEditWriter>();
    private static EditSeedFromViewModel Request() => new(1, 2, 3, " Сорт ", new() { AmountSeeds = 8 }, new());

    [Fact]
    public async Task Execute_ValidRequest_PassesCheckedModelAndCancellationToken()
    {
        _writer.SaveAsync(Arg.Any<SeedEdit>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SeedEditResultFromViewModel(1, 2)));
        using var cancellation = new CancellationTokenSource();
        var result = await new EditSeed(_writer).ExecuteAsync(Request(), cancellation.Token);
        Assert.Equal(new SeedEditResultFromViewModel(1, 2), result.ValueOrThrow());
        await _writer.Received(1).SaveAsync(Arg.Is<SeedEdit>(edit => edit.SeedId.Value == 1
            && edit.SortId.Value == 2 && edit.ProducerId.Value == 3 && edit.Characteristics.Name.Value == " Сорт "
            && edit.Package.Values.AmountSeeds == 8), cancellation.Token);
    }

    [Theory]
    [InlineData("seed")]
    [InlineData("sort")]
    [InlineData("producer")]
    [InlineData("package")]
    [InlineData("characteristics")]
    [InlineData("name")]
    public async Task Execute_InvalidRequest_DoesNotWrite(string field)
    {
        var request = field switch
        {
            "seed" => Request() with { SeedId = 0 },
            "sort" => Request() with { SortId = 0 },
            "producer" => Request() with { ProducerId = 0 },
            "package" => Request() with { Package = new() { AmountSeeds = -1 } },
            "name" => Request() with { SortName = "" },
            _ => Request() with { Characteristics = new() { YieldKg = Option.Some(double.NaN) } }
        };
        Assert.True((await new EditSeed(_writer).ExecuteAsync(request)).IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.SaveAsync(Arg.Any<SeedEdit>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SeedEditResultFromViewModel>("Производитель не найден"));
        var result = await new EditSeed(_writer).ExecuteAsync(Request());
        Assert.Equal("Производитель не найден", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_PreCancelledToken_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EditSeed(_writer).ExecuteAsync(Request(), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedEdit>(), Arg.Any<CancellationToken>());
    }
}
