using BonfireDB.Entities.Base;
using MoonCalendar;

namespace Services.Tests;

public class SeedlingsServiceTests
{
    private readonly IRepository<Seedling> _seedlings = Substitute.For<IRepository<Seedling>>();
    private readonly IRepository<SeedlingInfo> _seedlingsInfo = Substitute.For<IRepository<SeedlingInfo>>();
    private readonly IRepository<Replanting> _replantings = Substitute.For<IRepository<Replanting>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly MoonPhase _lunar = new();

    public SeedlingsServiceTests()
    {
        _uow.Repository<Seedling>().Returns(_seedlings);
        _uow.Repository<SeedlingInfo>().Returns(_seedlingsInfo);
        _uow.Repository<Replanting>().Returns(_replantings);
    }

    private SeedlingsService CreateService() => new(_uow.ToFactory(), _lunar);

    // ── MakeASeedling — Attach всего графа одним вызовом, единый SaveChanges ───

    [Fact]
    public async Task MakeASeedling_AddsSeedlingGraphAndSavesOnce()
    {
        var seedling = new Seedling { Id = 0, SeedlingInfos = [new SeedlingInfo { Id = 0 }] };
        _seedlings.AddAsync(seedling).Returns(seedling);

        var result = await CreateService().MakeASeedling(seedling);

        Assert.Same(seedling, result);
        await _seedlings.Received(1).AddAsync(seedling);
        await _uow.Received(1).SaveChangesAsync();
    }

    // ── UpdateSeedling ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSeedling_CallsRepositoryUpdate()
    {
        var seedling = new Seedling { Id = 1 };

        await CreateService().UpdateSeedling(seedling);

        await _seedlings.Received(1).UpdateAsync(seedling);
        await _uow.Received(1).SaveChangesAsync();
    }

    // ── DeleteSeedling ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSeedling_CallsRepositoryRemove()
    {
        var seedling = new Seedling { Id = 4 };

        await CreateService().DeleteSeedling(seedling);

        await _seedlings.Received(1).RemoveAsync(4);
    }

    // ── AddSeedlingInfo ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddSeedlingInfo_CallsRepositoryAdd()
    {
        var info = new SeedlingInfo { Id = 0 };
        _seedlingsInfo.AddAsync(info).Returns(info);

        var result = await CreateService().AddSeedlingInfo(info);

        Assert.Same(info, result);
        await _seedlingsInfo.Received(1).AddAsync(info);
    }

    // ── UpdateSeedlingInfo ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSeedlingInfo_NoReplants_OnlyUpdates()
    {
        var info = new SeedlingInfo { Id = 1, Replants = [] };

        await CreateService().UpdateSeedlingInfo(info);

        await _replantings.DidNotReceive().AddAsync(Arg.Any<Replanting>());
        await _seedlingsInfo.Received().UpdateAsync(info);
    }

    [Fact]
    public async Task UpdateSeedlingInfo_WithNewReplant_AddsReplant()
    {
        var newReplant = new Replanting { Id = 0, ReplantingDate = DateTime.Today };
        var info = new SeedlingInfo { Id = 1, Replants = [newReplant] };

        await CreateService().UpdateSeedlingInfo(info);

        await _replantings.Received(1).AddAsync(newReplant);
    }

    [Fact]
    public async Task UpdateSeedlingInfo_WithExistingReplant_SkipsAdd()
    {
        var existingReplant = new Replanting { Id = 5, ReplantingDate = DateTime.Today };
        var info = new SeedlingInfo { Id = 1, Replants = [existingReplant] };

        await CreateService().UpdateSeedlingInfo(info);

        await _replantings.DidNotReceive().AddAsync(Arg.Any<Replanting>());
    }

    [Fact]
    public async Task UpdateSeedlingInfo_MixedReplants_AddsOnlyNew()
    {
        var newReplant = new Replanting { Id = 0 };
        var existingReplant = new Replanting { Id = 3 };
        var info = new SeedlingInfo { Id = 1, Replants = [existingReplant, newReplant] };

        await CreateService().UpdateSeedlingInfo(info);

        await _replantings.Received(1).AddAsync(newReplant);
        await _replantings.DidNotReceive().AddAsync(existingReplant);
    }

    // ── MarkSeedlingInfosDeadAsync ────────────────────────────────────────────

    [Fact]
    public async Task MarkSeedlingInfosDeadAsync_SetsIsDeadAndNoteOnEachInfo()
    {
        var info1 = new SeedlingInfo { Id = 1 };
        var info2 = new SeedlingInfo { Id = 2 };
        var seedling = new Seedling { Id = 1 };

        await CreateService().MarkSeedlingInfosDeadAsync(seedling, [info1, info2], "погибли");

        Assert.True(info1.IsDead);
        Assert.True(info2.IsDead);
        Assert.Equal("погибли", info1.DeathNote);
        Assert.Equal("погибли", info2.DeathNote);
    }

    [Fact]
    public async Task MarkSeedlingInfosDeadAsync_UpdatesInfosAndSeedlingThenSavesOnce()
    {
        var info1 = new SeedlingInfo { Id = 1 };
        var info2 = new SeedlingInfo { Id = 2 };
        var seedling = new Seedling { Id = 1 };

        await CreateService().MarkSeedlingInfosDeadAsync(seedling, [info1, info2], null);

        await _seedlingsInfo.Received(1).UpdateAsync(info1);
        await _seedlingsInfo.Received(1).UpdateAsync(info2);
        await _seedlings.Received(1).UpdateAsync(seedling);
        await _uow.Received(1).SaveChangesAsync();
    }
}
