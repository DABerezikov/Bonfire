using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class SeedAcquisitionApplicationTests
{
    private readonly ISeedAcquisitionWriter _writer = Substitute.For<ISeedAcquisitionWriter>();
    private readonly FixedTime _time = new();
    private static AcquireSeedsFromViewModel Request() => new(new("Томат", "Сорт", "Фирма", "Овощи", "Куплено",
        SeedUnit.Pieces, 2, 3, 12m, new(2028, 12, 31), Option.None<string>()));

    [Fact]
    public async Task Execute_ValidInput_PassesCheckedAcquisitionTimeAndCancellation()
    {
        _writer.SaveAsync(Arg.Any<SeedAcquisition>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<AcquisitionResultFromViewModel>(new AcquisitionResultFromViewModel.Created(7)));
        using var cancellation = new CancellationTokenSource();
        var result = await new AcquireSeeds(_writer, _time).ExecuteAsync(Request(), cancellation.Token);
        Assert.Equal(7, result.ValueOrThrow().SeedId);
        await _writer.Received(1).SaveAsync(Arg.Is<SeedAcquisition>(value => value.Total.Value == 6),
            _time.GetLocalNow().DateTime, cancellation.Token);
    }

    [Fact]
    public async Task Execute_InvalidInput_DoesNotWrite()
    {
        var result = await new AcquireSeeds(_writer, _time).ExecuteAsync(new(Request().Values with { PackCount = -1 }));
        Assert.True(result.IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedAcquisition>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageFailure_PreservesReason()
    {
        _writer.SaveAsync(Arg.Any<SeedAcquisition>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AcquisitionResultFromViewModel>("Ошибка хранения"));
        var result = await new AcquireSeeds(_writer, _time).ExecuteAsync(Request());
        Assert.Equal("Ошибка хранения", result.Match(_ => "не ожидалось", error => error));
    }

    [Fact]
    public async Task Execute_PreCancelledToken_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AcquireSeeds(_writer, _time).ExecuteAsync(Request(), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedAcquisition>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 25, 12, 30, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
