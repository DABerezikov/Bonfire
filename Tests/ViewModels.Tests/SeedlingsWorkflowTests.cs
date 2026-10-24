using MoonCalendar;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace ViewModels.Tests;

public class SeedlingsWorkflowTests
{
    private readonly ISeedlingsService _service = Substitute.For<ISeedlingsService>();
    private readonly ISeedsService _seeds = Substitute.For<ISeedsService>();
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();
    private readonly IReportService _reports = Substitute.For<IReportService>();

    private static Seedling Batch(int id = 1) => new()
    {
        Id = id, SeedId = id, Plant = SeedsWorkflowTests.Seed(id).Plant, Quantity = 3,
        LandingDate = new DateTime(2026, 3, 1),
        SeedlingInfos = [new() { Id = 1, SeedlingNumber = 0, LandingDate = new DateTime(2026, 3, 1), LunarPhase = "Растущий серп", Replants = [], Treatments = [] }]
    };

    private SeedlingsViewModel CreateVm(params Seedling[] seedlings)
    {
        _service.GetAllSeedlingsAsync().Returns(seedlings);
        _seeds.GetAllSeedsAsync().Returns(new[] { SeedsWorkflowTests.Seed(1) });
        _service.Lunar.Returns(new MoonPhase());
        _service.UpdateSeedling(Arg.Any<Seedling>()).Returns(c => c.Arg<Seedling>());
        _service.MakeASeedling(Arg.Any<Seedling>()).Returns(c => { var entity = c.Arg<Seedling>(); entity.Id = 5; return entity; });
        _seeds.UpdateSeed(Arg.Any<Seed>()).Returns(c => c.Arg<Seed>());
        var vm = new SeedlingsViewModel(_service, _seeds, _dialog, _reports);
        vm.LoadDataCommand.Execute(null);
        if (seedlings.Length > 0) vm.SelectedSeedlingViewItem = vm.SeedlingsView!.Cast<SeedlingFromViewModel>().First();
        return vm;
    }

    [Fact]
    public void LoadAndFilters_PopulateInventoryAndHideDeadBatches()
    {
        var live = Batch(); var dead = Batch(2); dead.SeedlingInfos[0].IsDead = true;
        var vm = CreateVm(live, dead);
        Assert.Single(vm.AddPlantList);
        Assert.Single(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
        vm.IsHaving = false;
        Assert.Equal(2, vm.SeedlingsView!.Cast<SeedlingFromViewModel>().Count());
        vm.SeedlingFilter = "нет совпадений";
        Assert.Empty(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
        vm.SeedlingFilter = "-Выбрать все-";
        vm.SeedlingsChoiceClassCommand.Execute("Овощи");
        Assert.Equal(2, vm.SeedlingsView!.Cast<SeedlingFromViewModel>().Count());
        vm.SeedlingsChoiceClassCommand.Execute("Цветы");
        Assert.Empty(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateSeedling_ConsumesChosenUnitAndPreservesMetadata(bool grams)
    {
        var seed = SeedsWorkflowTests.Seed(1);
        if (grams) { seed.SeedsInfo.AmountSeeds = 0; seed.SeedsInfo.AmountSeedsWeight = 1.5; }
        _seeds.GetAllSeedsAsync().Returns(new[] { seed });
        _service.GetAllSeedlingsAsync().Returns(Array.Empty<Seedling>());
        _service.Lunar.Returns(new MoonPhase());
        var saved = Batch(2);
        saved.Quantity = grams ? 0 : 2;
        saved.Weight = grams ? 0.5 : 0;
        saved.PlantPlace = "Подоконник";
        saved.SeedlingInfos[0].SeedlingSource = PlantSources.FromSeeds;
        var refreshedSeed = SeedsWorkflowTests.Seed(1);
        refreshedSeed.SeedsInfo.AmountSeeds = grams ? 0 : 8;
        refreshedSeed.SeedsInfo.AmountSeedsWeight = grams ? 1 : seed.SeedsInfo.AmountSeedsWeight;
        _service.SowAsync(Arg.Any<SowingRequest>()).Returns(Result.Success(new SowingReceipt(saved, refreshedSeed)));
        var vm = new SeedlingsViewModel(_service, _seeds, _dialog, _reports);
        vm.LoadDataCommand.Execute(null);
        vm.AddCulture = "Томат";
        vm.CurrentPlant = vm.AddPlantList[0];
        vm.AddProducer = $"Фирма 1 {seed.SeedsInfo.ExpirationDate.Year}";
        vm.SeedlingSource = PlantSources.FromSeeds;
        vm.PlantingDate = new DateTime(2026, 3, 1);
        vm.AddPlantPlace = "Подоконник";
        vm.AddQuantity = grams ? 0.5 : 2;
        vm.AddOrCorrectSeedlingCommand.Execute(null);
        var added = Assert.Single(vm.Seedlings!);
        Assert.Equal("Подоконник", added.PlantPlace);
        Assert.Equal(new DateTime(2026, 3, 1), added.LandingDate);
        Assert.Equal(PlantSources.FromSeeds, added.SeedlingInfos[0].SeedlingSource);
        Assert.Equal(grams ? 0.5 : 0, added.Weight);
        Assert.Equal(grams ? 0 : 2, added.Quantity);
        Assert.Equal(grams ? 0 : 8, seed.SeedsInfo.AmountSeeds);
        if (grams) Assert.Equal(1, seed.SeedsInfo.AmountSeedsWeight);
        Assert.Null(vm.CurrentSeed);
        _service.Received(1).SowAsync(Arg.Is<SowingRequest>(request =>
            request.SeedId == 1 && request.Amount == (grams ? 0.5 : 2)
            && request.Unit == (grams ? SeedUnit.Grams : SeedUnit.Pieces)
            && request.Place == "Подоконник"));
        _ = _seeds.DidNotReceive().UpdateSeed(Arg.Any<Seed>());
    }

    [Fact]
    public async Task CreateSeedling_RejectedSowing_PreservesStockAndEditor()
    {
        var vm = CreateVm();
        var seed = (await _seeds.GetAllSeedsAsync()).Single();
        vm.AddCulture = "Томат";
        vm.CurrentPlant = vm.AddPlantList[0];
        vm.AddProducer = $"Фирма 1 {seed.SeedsInfo.ExpirationDate.Year}";
        vm.SeedlingSource = PlantSources.FromSeeds;
        vm.PlantingDate = new DateTime(2026, 3, 1);
        vm.AddQuantity = 2;
        _service.SowAsync(Arg.Any<SowingRequest>()).Returns(Result.Failure<SowingReceipt>("Остаток изменился"));

        vm.AddOrCorrectSeedlingCommand.Execute(null);

        Assert.Equal(10, seed.SeedsInfo.AmountSeeds);
        Assert.Empty(vm.Seedlings!);
        Assert.Same(seed, vm.CurrentSeed);
        Assert.Equal(2, vm.AddQuantity);
        _dialog.Received(1).Warning("Остаток изменился", Arg.Any<string>());
        _ = _seeds.DidNotReceive().UpdateSeed(Arg.Any<Seed>());
    }

    [Fact]
    public async Task CreateSeedling_PendingSowing_DoesNotMutateInventoryBeforeSuccess()
    {
        var vm = CreateVm();
        var seed = (await _seeds.GetAllSeedsAsync()).Single();
        vm.AddCulture = "Томат";
        vm.CurrentPlant = vm.AddPlantList[0];
        vm.AddProducer = $"Фирма 1 {seed.SeedsInfo.ExpirationDate.Year}";
        vm.SeedlingSource = PlantSources.FromSeeds;
        vm.PlantingDate = new DateTime(2026, 3, 1);
        vm.AddQuantity = 2;
        var pending = new TaskCompletionSource<Result<SowingReceipt>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = (Bonfire.Infrastructure.Commands.Base.CommandAsync)vm.AddOrCorrectSeedlingCommand;
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        _service.SowAsync(Arg.Any<SowingRequest>()).Returns(pending.Task);
        vm.AddOrCorrectSeedlingCommand.Execute(null);

        Assert.Equal(10, seed.SeedsInfo.AmountSeeds);
        Assert.Empty(vm.Seedlings!);
        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));

        pending.SetResult(Result.Failure<SowingReceipt>("Отменено"));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
        Assert.Equal(10, seed.SeedsInfo.AmountSeeds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteSeedling_ConfirmationControlsRemovalAndReturnToSeedStock(bool confirm)
    {
        var batch = Batch(); var vm = CreateVm(batch);
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(confirm);
        _service.DeleteSeedling(batch).Returns(batch);
        vm.DeleteSeedlingCommand.Execute(null);
        Assert.Equal(confirm ? 0 : 1, vm.Seedlings!.Count);
        _seeds.Received(confirm ? 1 : 0).ReturnSeedsFromSeedling(1, 3, 0);
    }

    [Fact]
    public void Germination_CapsNewSproutsAtSownQuantityAndNumbersThem()
    {
        var batch = Batch();
        batch.SeedlingInfos.Add(new() { SeedlingNumber = 1, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch);
        vm.Germinate = 20; vm.GerminationDate = new DateTime(2026, 3, 7);
        vm.GerminateSeedlingCommand.Execute(null);
        Assert.Equal(3, batch.SeedlingInfos.Count(i => i.GerminationDate.HasValue));
        Assert.Equal(new[] { 1, 2, 3 }, batch.SeedlingInfos.Where(i => i.GerminationDate.HasValue).Select(i => i.SeedlingNumber));
        Assert.Equal(0, vm.Germinate);
        _service.Received(2).AddSeedlingInfo(Arg.Any<SeedlingInfo>());
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void Germination_NonPositiveCount_LeavesBatchOrMarksUnsproutedDead(int count, bool dead)
    {
        var batch = Batch(); var vm = CreateVm(batch); vm.Germinate = count;
        vm.GerminateSeedlingCommand.Execute(null);
        Assert.Equal(dead, batch.SeedlingInfos[0].IsDead == true);
        _service.DidNotReceive().AddSeedlingInfo(Arg.Any<SeedlingInfo>());
    }

    [Fact]
    public void Replanting_RecordsDateAndCannotBeRepeatedForSameBatch()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.ReplantsDate = new DateTime(2026, 4, 1);
        vm.ReplantsSeedlingCommand.Execute(null);
        Assert.Equal(new DateTime(2026, 4, 1), Assert.Single(batch.SeedlingInfos[0].Replants!).ReplantingDate);
        Assert.False(vm.ReplantsSeedlingCommand.CanExecute(null));
        _service.Received(1).UpdateSeedlingInfo(batch.SeedlingInfos[0]);
    }

    [Fact]
    public void MarkDead_SelectsLastAliveSproutsAndPassesReason()
    {
        var batch = Batch();
        for (var number = 1; number <= 3; number++) batch.SeedlingInfos.Add(new() { SeedlingNumber = number, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch); vm.DeadNumbers = 2; vm.DeathNote = "Засохли";
        vm.DeathSeedlingCommand.Execute(null);
        _service.Received(1).MarkSeedlingInfosDeadAsync(batch,
            Arg.Is<IReadOnlyList<SeedlingInfo>>(infos => infos.Select(i => i.SeedlingNumber).SequenceEqual(new[] { 3, 2 })), "Засохли");
        Assert.Equal(0, vm.DeadNumbers); Assert.Equal(string.Empty, vm.DeathNote);
    }

    [Fact]
    public void Reports_PassDisplayedGerminationAndBalance()
    {
        var batch = Batch();
        batch.SeedlingInfos.Add(new() { SeedlingNumber = 1, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch); vm.IsActive = true;
        vm.CreateSeedlingsReportCommand.Execute(null);
        vm.CreateFilteredSeedlingsReportCommand.Execute(null);
        _reports.Received().CreateSeedlingsReport(Arg.Is<IEnumerable<SeedlingReportItem>>(rows => rows.Single().CountGerminate == 1 && rows.Single().Balance == 1), "Рассада");
        _reports.Received().CreateSeedlingsReport(Arg.Is<IEnumerable<SeedlingReportItem>>(rows => rows.Single().Culture == "Томат"), "Рассада_выборка");
    }
}
