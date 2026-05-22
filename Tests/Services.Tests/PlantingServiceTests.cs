using Bonfire.Services.Interfaces;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using MoonCalendar;

namespace Services.Tests;

public class PlantingServiceTests
{
    private readonly IGardenService _gardenService = Substitute.For<IGardenService>();
    private readonly ISeedlingsService _seedlingsService = Substitute.For<ISeedlingsService>();
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();

    private static readonly DateTime PlantedDate = new(2026, 5, 1);

    private PlantingService CreateService() =>
        new(_gardenService, _seedlingsService, _seedsService);

    private static PlantRequest SeedlingRequest(int spotId = 1, int entityId = 5,
        bool weight = false, double amount = 3) =>
        new(spotId, PlantSourceKind.Seedling, entityId, weight, amount,
            PlantedDate, "Томат Черри", "План / Участок / Грядка");

    private static PlantRequest SeedRequest(int spotId = 1, int entityId = 8,
        bool weight = false, double amount = 5) =>
        new(spotId, PlantSourceKind.Seed, entityId, weight, amount,
            PlantedDate, "Огурец Кураж", "План / Участок / Грядка");

    // ── Ячейка не найдена / недопустимый переход ──────────────────────────────

    [Fact]
    public async Task PlantAsync_SpotNotFound_ReturnsFailure()
    {
        _gardenService.GetSpotAsync(1).Returns((PlantingSpot?)null);

        var result = await CreateService().PlantAsync(SeedlingRequest());

        Assert.False(result.Success);
        Assert.Null(result.SeedlingInfoId);
    }

    [Fact]
    public async Task PlantAsync_InvalidTransition_ReturnsFailureAndDoesNotChangeState()
    {
        // Planted → Planted запрещён
        var spot = new PlantingSpot { Id = 1, StateTypeName = nameof(PlantedSpotState) };
        _gardenService.GetSpotAsync(1).Returns(spot);

        var result = await CreateService().PlantAsync(SeedlingRequest());

        Assert.False(result.Success);
        await _gardenService.DidNotReceive().ChangeSpotStateAsync(
            Arg.Any<PlantingSpot>(), Arg.Any<PlantingSpotState>(),
            Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<int?>());
        await _seedlingsService.DidNotReceive().GetSeedlingAsync(Arg.Any<int>());
    }

    // ── Посадка из рассады ────────────────────────────────────────────────────

    [Fact]
    public async Task PlantAsync_FromSeedling_DeductsQuantity()
    {
        var spot = new PlantingSpot { Id = 1 };
        var seedling = new Seedling { Id = 5, Quantity = 10 };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedlingsService.GetSeedlingAsync(5).Returns(seedling);
        _seedlingsService.AddSeedlingInfo(Arg.Any<SeedlingInfo>())
            .Returns(ci => { var i = ci.Arg<SeedlingInfo>(); i.Id = 99; return i; });

        var result = await CreateService().PlantAsync(SeedlingRequest(amount: 3));

        Assert.True(result.Success);
        Assert.Equal(99, result.SeedlingInfoId);
        Assert.Equal(7, seedling.Quantity);
        await _seedlingsService.Received(1).UpdateSeedling(seedling);
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_DeductsWeight()
    {
        var spot = new PlantingSpot { Id = 1 };
        var seedling = new Seedling { Id = 5, Weight = 12.5 };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedlingsService.GetSeedlingAsync(5).Returns(seedling);
        _seedlingsService.AddSeedlingInfo(Arg.Any<SeedlingInfo>())
            .Returns(ci => { var i = ci.Arg<SeedlingInfo>(); i.Id = 1; return i; });

        await CreateService().PlantAsync(SeedlingRequest(weight: true, amount: 4.5));

        Assert.Equal(8.0, seedling.Weight);
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_AddsInfoWithPlantPlaceAndChangesSpotState()
    {
        var spot = new PlantingSpot { Id = 1 };
        var seedling = new Seedling { Id = 5, Quantity = 10 };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedlingsService.GetSeedlingAsync(5).Returns(seedling);
        SeedlingInfo? captured = null;
        _seedlingsService.AddSeedlingInfo(Arg.Do<SeedlingInfo>(i => captured = i))
            .Returns(ci => { var i = ci.Arg<SeedlingInfo>(); i.Id = 42; return i; });

        await CreateService().PlantAsync(SeedlingRequest());

        Assert.NotNull(captured);
        Assert.Equal("План / Участок / Грядка", captured!.PlantPlace);
        Assert.Contains(captured, seedling.SeedlingInfos);
        await _gardenService.Received(1).ChangeSpotStateAsync(
            Arg.Is(spot), Arg.Any<PlantedSpotState>(),
            Arg.Is("Томат Черри"), Arg.Is<DateTime?>(PlantedDate), Arg.Is<int?>(42));
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_SeedlingNotFound_ReturnsFailure()
    {
        var spot = new PlantingSpot { Id = 1 };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedlingsService.GetSeedlingAsync(5).Returns((Seedling?)null);

        var result = await CreateService().PlantAsync(SeedlingRequest());

        Assert.False(result.Success);
        await _gardenService.DidNotReceive().ChangeSpotStateAsync(
            Arg.Any<PlantingSpot>(), Arg.Any<PlantingSpotState>(),
            Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<int?>());
    }

    // ── Посадка из семян ──────────────────────────────────────────────────────

    [Fact]
    public async Task PlantAsync_FromSeed_CreatesSeedlingAndDeductsSeeds()
    {
        var spot = new PlantingSpot { Id = 1 };
        var seed = new Seed
        {
            Id = 8,
            Plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Огурец" },
                PlantSort = new PlantSort { Name = "Кураж", Producer = new Producer { Name = "Г" } }
            },
            SeedsInfo = new SeedsInfo { AmountSeeds = 20 }
        };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedsService.GetSeedAsync(8).Returns(seed);
        _seedlingsService.Lunar.Returns(new MoonPhase());
        _seedlingsService.MakeASeedling(Arg.Any<Seedling>())
            .Returns(ci => { var s = ci.Arg<Seedling>(); s.SeedlingInfos[0].Id = 77; return s; });

        var result = await CreateService().PlantAsync(SeedRequest(amount: 5));

        Assert.True(result.Success);
        Assert.Equal(77, result.SeedlingInfoId);
        Assert.Equal(15, seed.SeedsInfo.AmountSeeds);
        await _seedlingsService.Received(1).MakeASeedling(Arg.Any<Seedling>());
        await _seedsService.Received(1).UpdateSeed(seed);
    }

    [Fact]
    public async Task PlantAsync_FromSeed_DeductsWeight()
    {
        var spot = new PlantingSpot { Id = 1 };
        var seed = new Seed
        {
            Id = 8,
            Plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Огурец" },
                PlantSort = new PlantSort { Name = "Кураж", Producer = new Producer { Name = "Г" } }
            },
            SeedsInfo = new SeedsInfo { AmountSeedsWeight = 10.0 }
        };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedsService.GetSeedAsync(8).Returns(seed);
        _seedlingsService.Lunar.Returns(new MoonPhase());
        _seedlingsService.MakeASeedling(Arg.Any<Seedling>())
            .Returns(ci => { var s = ci.Arg<Seedling>(); s.SeedlingInfos[0].Id = 1; return s; });

        await CreateService().PlantAsync(SeedRequest(weight: true, amount: 4));

        Assert.Equal(6.0, seed.SeedsInfo.AmountSeedsWeight);
    }

    [Fact]
    public async Task PlantAsync_FromSeed_SeedNotFound_ReturnsFailure()
    {
        var spot = new PlantingSpot { Id = 1 };
        _gardenService.GetSpotAsync(1).Returns(spot);
        _seedsService.GetSeedAsync(8).Returns((Seed?)null);

        var result = await CreateService().PlantAsync(SeedRequest());

        Assert.False(result.Success);
        await _gardenService.DidNotReceive().ChangeSpotStateAsync(
            Arg.Any<PlantingSpot>(), Arg.Any<PlantingSpotState>(),
            Arg.Any<string?>(), Arg.Any<DateTime?>(), Arg.Any<int?>());
    }
}
