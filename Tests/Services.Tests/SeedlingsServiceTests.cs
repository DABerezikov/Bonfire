using MoonCalendar;

namespace Services.Tests;

public class SeedlingsServiceTests
{
    private readonly IRepository<Plant> _plants = Substitute.For<IRepository<Plant>>();
    private readonly IRepository<Seedling> _seedlings = Substitute.For<IRepository<Seedling>>();
    private readonly IRepository<PlantSort> _sort = Substitute.For<IRepository<PlantSort>>();
    private readonly IRepository<PlantCulture> _culture = Substitute.For<IRepository<PlantCulture>>();
    private readonly IRepository<Producer> _producer = Substitute.For<IRepository<Producer>>();
    private readonly IRepository<SeedlingInfo> _seedlingsInfo = Substitute.For<IRepository<SeedlingInfo>>();
    private readonly IRepository<Replanting> _replantings = Substitute.For<IRepository<Replanting>>();
    private readonly IRepository<Treatment> _treatments = Substitute.For<IRepository<Treatment>>();
    private readonly MoonPhase _lunar = new();

    private SeedlingsService CreateService() =>
        new(_plants, _seedlings, _sort, _culture, _producer,
            _seedlingsInfo, _replantings, _treatments, _lunar);

    // ── MakeASeedling ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MakeASeedling_AddsEachSeedlingInfoThenSeedling()
    {
        var info1 = new SeedlingInfo { Id = 0 };
        var info2 = new SeedlingInfo { Id = 0 };
        var seedling = new Seedling
        {
            Id = 0,
            SeedlingInfos = [info1, info2]
        };
        _seedlings.AddAsync(seedling).Returns(seedling);

        var service = CreateService();
        await service.MakeASeedling(seedling);

        await _seedlingsInfo.Received(1).AddAsync(info1);
        await _seedlingsInfo.Received(1).AddAsync(info2);
        await _seedlings.Received(1).AddAsync(seedling);
    }

    [Fact]
    public async Task MakeASeedling_EmptySeedlingInfos_OnlyAddsSeedling()
    {
        var seedling = new Seedling { Id = 0, SeedlingInfos = [] };
        _seedlings.AddAsync(seedling).Returns(seedling);

        var service = CreateService();
        await service.MakeASeedling(seedling);

        await _seedlingsInfo.DidNotReceive().AddAsync(Arg.Any<SeedlingInfo>());
        await _seedlings.Received(1).AddAsync(seedling);
    }

    // ── UpdateSeedling ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSeedling_CallsRepositoryUpdateAsync()
    {
        var seedling = new Seedling { Id = 1 };
        var service = CreateService();

        await service.UpdateSeedling(seedling);

        await _seedlings.Received(1).UpdateAsync(seedling);
    }

    // ── DeleteSeedling ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSeedling_CallsRepositoryRemoveAsync()
    {
        var seedling = new Seedling { Id = 4 };
        var service = CreateService();

        await service.DeleteSeedling(seedling);

        await _seedlings.Received(1).RemoveAsync(4);
    }

    // ── AddSeedlingInfo ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddSeedlingInfo_CallsRepositoryAddAsync()
    {
        var info = new SeedlingInfo { Id = 0 };
        var added = new SeedlingInfo { Id = 1 };
        _seedlingsInfo.AddAsync(info).Returns(added);

        var service = CreateService();
        var result = await service.AddSeedlingInfo(info);

        Assert.Equal(added, result);
        await _seedlingsInfo.Received(1).AddAsync(info);
    }

    // ── UpdateSeedlingInfo ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSeedlingInfo_NoReplants_OnlyCallsUpdateAsync()
    {
        var info = new SeedlingInfo { Id = 1, Replants = [] };
        var service = CreateService();

        await service.UpdateSeedlingInfo(info);

        await _replantings.DidNotReceive().AddAsync(Arg.Any<Replanting>());
        await _seedlingsInfo.Received().UpdateAsync(info);
    }

    [Fact]
    public async Task UpdateSeedlingInfo_WithNewReplant_AddsReplant()
    {
        var newReplant = new Replanting { Id = 0, ReplantingDate = DateTime.Today };
        var info = new SeedlingInfo
        {
            Id = 1,
            Replants = [newReplant]
        };
        var service = CreateService();

        await service.UpdateSeedlingInfo(info);

        await _replantings.Received(1).AddAsync(newReplant);
    }

    [Fact]
    public async Task UpdateSeedlingInfo_WithExistingReplant_SkipsAdd()
    {
        var existingReplant = new Replanting { Id = 5, ReplantingDate = DateTime.Today };
        var info = new SeedlingInfo
        {
            Id = 1,
            Replants = [existingReplant]
        };
        var service = CreateService();

        await service.UpdateSeedlingInfo(info);

        await _replantings.DidNotReceive().AddAsync(Arg.Any<Replanting>());
    }

    [Fact]
    public async Task UpdateSeedlingInfo_MixedReplants_AddsOnlyNew()
    {
        var newReplant = new Replanting { Id = 0 };
        var existingReplant = new Replanting { Id = 3 };
        var info = new SeedlingInfo
        {
            Id = 1,
            Replants = [existingReplant, newReplant]
        };
        var service = CreateService();

        await service.UpdateSeedlingInfo(info);

        await _replantings.Received(1).AddAsync(newReplant);
        await _replantings.DidNotReceive().AddAsync(existingReplant);
    }

    // ── InvertAutoSave ────────────────────────────────────────────────────────

    [Fact]
    public void InvertAutoSave_TogglesAllEightRepositories()
    {
        _plants.AutoSaveChanges.Returns(true);
        _sort.AutoSaveChanges.Returns(true);
        _seedlings.AutoSaveChanges.Returns(true);
        _culture.AutoSaveChanges.Returns(true);
        _producer.AutoSaveChanges.Returns(true);
        _seedlingsInfo.AutoSaveChanges.Returns(true);
        _replantings.AutoSaveChanges.Returns(true);
        _treatments.AutoSaveChanges.Returns(true);

        var service = CreateService();
        service.InvertAutoSave();

        _plants.Received(1).AutoSaveChanges = false;
        _sort.Received(1).AutoSaveChanges = false;
        _seedlings.Received(1).AutoSaveChanges = false;
        _culture.Received(1).AutoSaveChanges = false;
        _producer.Received(1).AutoSaveChanges = false;
        _seedlingsInfo.Received(1).AutoSaveChanges = false;
        _replantings.Received(1).AutoSaveChanges = false;
        _treatments.Received(1).AutoSaveChanges = false;
    }
}
