using Bonfire.Application.Garden;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class PlantingApplicationTests
{
    private readonly IPlantingWriter _writer = Substitute.For<IPlantingWriter>();
    private readonly ILunarCalendar _calendar = Substitute.For<ILunarCalendar>();
    private static PlantInSpotFromViewModel Request(int spotId = 4, double amount = 2) => new(spotId,
        new PlantingSource.Seeds(SeedId.Create(7).ValueOrThrow(), SeedUnit.Grams), amount, new(2026, 5, 1), " Томат ", Option.Some(""));

    [Fact]
    public async Task Execute_ValidRequest_ForwardsCheckedOrderCalendarAndToken()
    {
        var request = Request();
        _calendar.GetPhase(request.Date).Returns("Фаза");
        var saved = new PlantingResultFromViewModel(4, 9, SeedAmount.Create(2, SeedUnit.Grams).ValueOrThrow(), SeedAmount.Create(1, SeedUnit.Grams).ValueOrThrow());
        _writer.ApplyAsync(Arg.Any<SpotPlanting>(), "Фаза", Arg.Any<CancellationToken>()).Returns(Result.Success(saved));
        using var cancellation = new CancellationTokenSource();
        Assert.Equal(saved, (await new PlantInSpot(_writer, _calendar).ExecuteAsync(request, cancellation.Token)).ValueOrThrow());
        await _writer.Received(1).ApplyAsync(Arg.Is<SpotPlanting>(order => order.SpotId == 4 && order.Amount.Unit == SeedUnit.Grams
            && order.Amount.Value == 2 && order.Label == " Томат " && order.Place.Equals(Option.Some(""))), "Фаза", cancellation.Token);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(4, 0)]
    [InlineData(4, -1)]
    [InlineData(4, double.NaN)]
    [InlineData(4, double.PositiveInfinity)]
    public async Task Execute_InvalidRequest_DoesNotCalculateOrWrite(int spotId, double amount)
    {
        Assert.True((await new PlantInSpot(_writer, _calendar).ExecuteAsync(Request(spotId, amount))).IsFailure);
        _calendar.DidNotReceive().GetPhase(Arg.Any<DateTime>());
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<SpotPlanting>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Cancelled_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PlantInSpot(_writer, _calendar).ExecuteAsync(Request(), cancellation.Token));
        await _writer.DidNotReceive().ApplyAsync(Arg.Any<SpotPlanting>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_StorageRefused_PreservesError()
    {
        _calendar.GetPhase(Arg.Any<DateTime>()).Returns("Фаза");
        _writer.ApplyAsync(Arg.Any<SpotPlanting>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<PlantingResultFromViewModel>("Ячейка занята"));
        Assert.Equal("Ячейка занята", (await new PlantInSpot(_writer, _calendar).ExecuteAsync(Request())).Match(_ => "", error => error));
    }
}
