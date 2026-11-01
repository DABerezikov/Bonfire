using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace Services.Tests;

public class PlanningLibraryApplicationTests
{
    private readonly IPlanningLibraryWriter _writer = Substitute.For<IPlanningLibraryWriter>();

    [Fact]
    public async Task Region_InvalidIdOrZone_DoesNotWrite()
    {
        var app = new SavePlanningLibraries(_writer);
        Assert.True((await app.SaveRegionAsync(new(Option.Some(0), "Томск", "Asia/Tomsk", RegionReference.Empty, false))).IsFailure);
        Assert.True((await app.SaveRegionAsync(new(Option.None<int>(), "Томск", "invalid-zone", RegionReference.Empty, false))).IsFailure);
        await _writer.DidNotReceive().SaveRegionAsync(Arg.Any<Option<RegionId>>(), Arg.Any<RegionDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Region_Create_PassesNoneAndReturnsStorageFailure()
    {
        _writer.SaveRegionAsync(Arg.Any<Option<RegionId>>(), Arg.Any<RegionDefinition>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RegionFromViewModel>("Отказ хранилища"));
        var result = await new SavePlanningLibraries(_writer).SaveRegionAsync(new(Option.None<int>(), "Томск", "Asia/Tomsk", RegionReference.Empty, false));
        Assert.Equal("Отказ хранилища", result.Match(_ => "нет отказа", error => error));
        await _writer.Received(1).SaveRegionAsync(Arg.Is<Option<RegionId>>(x => x is Option<RegionId>.None),
            Arg.Is<RegionDefinition>(x => x.Name.Value == "Томск"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Profiles_InvalidIdentityOrLunarGroup_DoesNotWrite()
    {
        var app = new SavePlanningLibraries(_writer);
        Assert.True((await app.SaveCultureProfileAsync(new(0, new(), new()))).IsFailure);
        Assert.True((await app.SaveSortSettingsAsync(new(1, new() { LunarGroup = Option.Some((LunarGroup)99) }))).IsFailure);
        await _writer.DidNotReceive().SaveCultureProfileAsync(Arg.Any<CultureGrowingProfile>(), Arg.Any<CancellationToken>());
        await _writer.DidNotReceive().SaveSortSettingsAsync(Arg.Any<SortPlanningSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelledRequest_DoesNotWrite()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SavePlanningLibraries(_writer)
            .SaveRegionAsync(new(Option.None<int>(), "Томск", "Asia/Tomsk", RegionReference.Empty, false), cancellation.Token));
        Assert.Empty(_writer.ReceivedCalls());
    }
}
