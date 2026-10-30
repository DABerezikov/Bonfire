using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Services.Tests;

public class SeedlingEditingApplicationTests
{
    private readonly ISeedlingEditWriter _writer = Substitute.For<ISeedlingEditWriter>();
    private readonly ILunarCalendar _calendar = Substitute.For<ILunarCalendar>();
    private static SeedlingDetailsValues Values() => new() { Quantity = 3,
        LandingDate = Option.Some(new DateTime(2026, 4, 1)), Source = Option.Some("Из семян") };

    [Fact]
    public async Task Execute_RecalculatesPhaseAndForwardsCheckedEdit()
    {
        using var cancellation = new CancellationTokenSource();
        _calendar.GetPhase(new(2026, 4, 1)).Returns("Новая фаза");
        var details = SeedlingDetails.Create(Values()).ValueOrThrow();
        _writer.SaveAsync(Arg.Any<SeedlingEdit>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SeedlingEditResultFromViewModel(details, new(7, []))));
        var result = await new EditSeedling(_writer, _calendar).ExecuteAsync(new(7, Values(), []), cancellation.Token);
        Assert.Equal(7, result.ValueOrThrow().Change.SeedlingId);
        await _writer.Received(1).SaveAsync(Arg.Is<SeedlingEdit>(edit => edit.Id.Value == 7 && edit.Details.Values.LunarPhase == "Новая фаза"), cancellation.Token);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(7, -1)]
    public async Task Execute_InvalidInput_DoesNotWrite(int id, double quantity)
    {
        Assert.True((await new EditSeedling(_writer, _calendar).ExecuteAsync(new(id, Values() with { Quantity = quantity }, []))).IsFailure);
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedlingEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_MissingDate_DoesNotWriteOrCalculate()
    {
        Assert.True((await new EditSeedling(_writer, _calendar).ExecuteAsync(new(7, Values() with { LandingDate = Option.None<DateTime>() }, []))).IsFailure);
        _calendar.DidNotReceive().GetPhase(Arg.Any<DateTime>());
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedlingEdit>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EditSeedling(_writer, _calendar).ExecuteAsync(new(7, Values(), []), cancellation.Token));
        await _writer.DidNotReceive().SaveAsync(Arg.Any<SeedlingEdit>(), Arg.Any<CancellationToken>());
    }
}
