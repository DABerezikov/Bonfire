using MoonCalendar;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Seedlings;

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
        _service.ReadSeedlingsAsync().Returns(seedlings.ReadSeedlings());
        _seeds.ReadSeedsAsync().Returns(new[] { SeedsWorkflowTests.Seed(1) }.ReadSeeds());
        _service.Lunar.Returns(new MoonPhase());
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
        _seeds.ReadSeedsAsync().Returns(new[] { seed }.ReadSeeds());
        _service.ReadSeedlingsAsync().Returns(Array.Empty<Seedling>().ReadSeedlings());
        _service.Lunar.Returns(new MoonPhase());
        var saved = Batch(2);
        saved.Quantity = grams ? 0 : 2;
        saved.Weight = grams ? 0.5 : 0;
        saved.PlantPlace = "Подоконник";
        saved.SeedlingInfos[0].SeedlingSource = PlantSources.FromSeeds;
        var refreshedSeed = SeedsWorkflowTests.Seed(1);
        refreshedSeed.SeedsInfo.AmountSeeds = grams ? 0 : 8;
        refreshedSeed.SeedsInfo.AmountSeedsWeight = grams ? 1 : seed.SeedsInfo.AmountSeedsWeight;
        var details = new SowingDetails(new DateTime(2026, 3, 1), PlantSources.FromSeeds, Option.Some("Подоконник"), "Фаза");
        var gramsLeft = refreshedSeed.SeedsInfo.AmountSeedsWeight.ToOption();
        _service.SowAsync(Arg.Any<SowingRequest>()).Returns(Result.Success(new SowingReceipt(1,
            SeedAmount.Create(grams ? 0.5 : 2, grams ? SeedUnit.Grams : SeedUnit.Pieces).ValueOrThrow(), details,
            new SavedSowing(saved.Id, refreshedSeed.SeedsInfo.AmountSeeds, gramsLeft))));
        _service.ReadSeedlingAsync(saved.Id).Returns(Result.Success(Option.Some(new[] { saved }.ReadSeedlings().ValueOrThrow().Single())));
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
        Assert.Equal(grams ? 0 : 10, seed.SeedsInfo.AmountSeeds);
        if (grams) Assert.Equal(1.5, seed.SeedsInfo.AmountSeedsWeight);
        Assert.Null(vm.CurrentSeed);
        vm.CurrentPlant = Assert.Single(vm.AddPlantList);
        Assert.Equal(grams ? 0 : 8, vm.CurrentSeed!.SeedsInfo.AmountSeeds);
        if (grams) Assert.Equal(1, vm.CurrentSeed.SeedsInfo.AmountSeedsWeight.ToNullable());
        _service.Received(1).SowAsync(Arg.Is<SowingRequest>(request =>
            request.SeedId == 1 && request.Amount == (grams ? 0.5 : 2)
            && request.Unit == (grams ? SeedUnit.Grams : SeedUnit.Pieces)
            && request.Place.Match(value => value == "Подоконник", () => false)));
    }

    [Fact]
    public async Task CreateSeedling_RejectedSowing_PreservesStockAndEditor()
    {
        var vm = CreateVm();
        var seed = (await _seeds.ReadSeedsAsync()).ValueOrThrow().Single();
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
    }

    [Fact]
    public async Task CreateSeedling_PendingSowing_DoesNotMutateInventoryBeforeSuccess()
    {
        var vm = CreateVm();
        var seed = (await _seeds.ReadSeedsAsync()).ValueOrThrow().Single();
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

    [Fact]
    public void CreateSeedling_SavedBatchNoLongerAvailable_DoesNotLeaveResubmittableEditor()
    {
        var vm = CreateVm();
        vm.AddCulture = "Томат";
        vm.CurrentPlant = vm.AddPlantList[0];
        vm.AddProducer = $"Фирма 1 {vm.CurrentSeed!.SeedsInfo.ExpirationDate.Year}";
        vm.SeedlingSource = PlantSources.FromSeeds;
        vm.PlantingDate = new DateTime(2026, 3, 1);
        vm.AddQuantity = 2;
        var details = new SowingDetails(vm.PlantingDate, PlantSources.FromSeeds, Option.None<string>(), "Фаза");
        _service.SowAsync(Arg.Any<SowingRequest>()).Returns(Result.Success(new SowingReceipt(1,
            SeedAmount.Create(2, SeedUnit.Pieces).ValueOrThrow(), details, new SavedSowing(42, 8, Option.None<double>()))));
        _service.ReadSeedlingAsync(42).Returns(Result.Success(Option.None<SeedlingReadFromViewModel>()));

        vm.AddOrCorrectSeedlingCommand.Execute(null);

        Assert.Empty(vm.Seedlings!);
        Assert.Null(vm.CurrentSeed);
        Assert.Equal(0, vm.AddQuantity);
        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
        _dialog.Received(1).Warning(Arg.Is<string>(message => message.Contains("Посев сохранён")), "Посев рассады");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DeleteSeedling_ChoiceControlsRemovalAndReturnToSeedStock(int choice)
    {
        var batch = Batch(); var vm = CreateVm(batch);
        SeedlingRemoval removal = choice == 1 ? new SeedlingRemoval.CorrectSowing() : new SeedlingRemoval.Discard();
        _dialog.RequestSeedlingRemoval(Arg.Any<string>()).Returns(choice == 0 ? Option.None<SeedlingRemoval>() : Option.Some(removal));
        var stock = choice == 1 ? Option.Some(new ReturnedSeedStockFromViewModel(1, 13, Option.Some(0.5))) : Option.None<ReturnedSeedStockFromViewModel>();
        _service.DeleteAsync(Arg.Any<DeleteSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingDeletionResultFromViewModel(1, stock)));
        vm.DeleteSeedlingCommand.Execute(null);
        Assert.Equal(choice == 0 ? 1 : 0, vm.Seedlings!.Count);
        _service.Received(choice == 0 ? 0 : 1).DeleteAsync(Arg.Is<DeleteSeedlingFromViewModel>(request => request.SeedlingId == 1 && request.Removal == removal));
        if (choice != 0) Assert.Null(vm.SelectedItem);
        Assert.Contains("-Выбрать все-", vm.ListCulture);
    }

    [Fact]
    public void DeleteSeedling_Failure_PreservesBatchSelectionAndDraft()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        var draft = vm.EditedItem;
        _dialog.RequestSeedlingRemoval(Arg.Any<string>()).Returns(Option.Some<SeedlingRemoval>(new SeedlingRemoval.CorrectSowing()));
        _service.DeleteAsync(Arg.Any<DeleteSeedlingFromViewModel>()).Returns(Result.Failure<SeedlingDeletionResultFromViewModel>("Исходный пакет удалён"));
        vm.DeleteSeedlingCommand.Execute(null);
        Assert.Equal(batch.Id, Assert.Single(vm.Seedlings!).Id);
        Assert.Same(vm.Seedlings![0], vm.SelectedItem);
        Assert.Same(draft, vm.EditedItem);
        _dialog.Received(1).Warning("Исходный пакет удалён", "Удаление рассады");
    }

    [Fact]
    public void DeleteSeedling_Correction_UpdatesStockAndRestoresDepletedPackageToSuggestions()
    {
        var batch = Batch();
        var seed = SeedsWorkflowTests.Seed(1);
        seed.SeedsInfo.AmountSeeds = 0;
        seed.SeedsInfo.AmountSeedsWeight = 0;
        _seeds.ReadSeedsAsync().Returns(new[] { seed }.ReadSeeds());
        _service.ReadSeedlingsAsync().Returns(new[] { batch }.ReadSeedlings());
        _service.Lunar.Returns(new MoonPhase());
        var vm = new SeedlingsViewModel(_service, _seeds, _dialog, _reports);
        vm.LoadDataCommand.Execute(null);
        vm.SelectedSeedlingViewItem = Assert.Single(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
        Assert.Empty(vm.AddPlantList);
        _dialog.RequestSeedlingRemoval(Arg.Any<string>()).Returns(Option.Some<SeedlingRemoval>(new SeedlingRemoval.CorrectSowing()));
        _service.DeleteAsync(Arg.Any<DeleteSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingDeletionResultFromViewModel(1,
            Option.Some(new ReturnedSeedStockFromViewModel(1, 3, Option.Some(0.0))))));

        vm.DeleteSeedlingCommand.Execute(null);

        Assert.Empty(vm.Seedlings!);
        Assert.Equal(0, seed.SeedsInfo.AmountSeeds);
        Assert.Equal(1, Assert.Single(vm.AddPlantList).Id);
        vm.CurrentPlant = vm.AddPlantList[0];
        Assert.Equal(3, vm.CurrentSeed!.SeedsInfo.AmountSeeds);
        Assert.Single(vm.AddSortList);
        Assert.Single(vm.AddCultureList);
    }

    [Fact]
    public void Germination_CapsNewSproutsAtSownQuantityAndNumbersThem()
    {
        var batch = Batch();
        batch.SeedlingInfos.Add(new() { Id = 2, SeedlingNumber = 1, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch);
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingChangeResultFromViewModel(batch.Id,
            [new(1, 0, Option.None<DateTime>(), Option.None<bool>()),
             new(2, 1, Option.Some(DateTime.Today), Option.None<bool>()),
             new(3, 2, Option.Some(new DateTime(2026, 3, 7)), Option.None<bool>()),
             new(4, 3, Option.Some(new DateTime(2026, 3, 7)), Option.None<bool>())])));
        vm.Germinate = 20; vm.GerminationDate = new DateTime(2026, 3, 7);
        vm.GerminateSeedlingCommand.Execute(null);
        Assert.Equal(3, vm.SelectedItem!.SeedlingInfos.Count(i => i.GerminationDate.HasValue));
        Assert.Equal(new[] { 1, 2, 3 }, vm.SelectedItem!.SeedlingInfos.Where(i => i.GerminationDate.HasValue).Select(i => i.SeedlingNumber));
        Assert.Equal(0, vm.Germinate);
        _service.Received(1).ChangeAsync(Arg.Is<ChangeSeedlingFromViewModel>(request => request.Action == new SeedlingAction.Germinate(20, new DateTime(2026, 3, 7))));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void Germination_NonPositiveCount_LeavesBatchOrMarksUnsproutedDead(int count, bool dead)
    {
        var batch = Batch(); var vm = CreateVm(batch); vm.Germinate = count;
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingChangeResultFromViewModel(batch.Id,
            [new(1, 0, Option.None<DateTime>(), Option.Some(true))])));
        vm.GerminateSeedlingCommand.Execute(null);
        Assert.Equal(dead, vm.SelectedItem!.SeedlingInfos[0].IsDead == true);
    }

    [Fact]
    public void Germination_SaveFailure_PreservesBatchInputAndDraft()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.Germinate = 3;
        var date = vm.GerminationDate;
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Failure<SeedlingChangeResultFromViewModel>("Ошибка всходов"));
        vm.GerminateSeedlingCommand.Execute(null);
        Assert.Single(vm.SelectedItem!.SeedlingInfos);
        Assert.Equal(3, vm.Germinate);
        Assert.Equal(date, vm.GerminationDate);
        Assert.Single(vm.EditedItem!.Entries);
        _dialog.Received(1).Warning("Ошибка всходов", "Всходы рассады");
    }

    [Fact]
    public void Replanting_RecordsDateAndCannotBeRepeatedForSameBatch()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.ReplantsDate = new DateTime(2026, 4, 1);
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingChangeResultFromViewModel(batch.Id,
            [new(1, 0, Option.None<DateTime>(), Option.None<bool>())
            { Replants = [new(1, new DateTime(2026, 4, 1), Option.None<double>(), Option.None<string>())] }])));
        vm.ReplantsSeedlingCommand.Execute(null);
        Assert.Equal(new DateTime(2026, 4, 1), Assert.Single(vm.SelectedItem!.SeedlingInfos[0].Replants!).ReplantingDate);
        Assert.False(vm.ReplantsSeedlingCommand.CanExecute(null));
        _service.Received(1).ChangeAsync(Arg.Is<ChangeSeedlingFromViewModel>(request => request.Action == new SeedlingAction.Replant(new DateTime(2026, 4, 1))));
    }

    [Fact]
    public void Replanting_SaveFailure_PreservesBatchAndDate()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.ReplantsDate = new DateTime(2026, 4, 1);
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Failure<SeedlingChangeResultFromViewModel>("Ошибка пересадки"));
        vm.ReplantsSeedlingCommand.Execute(null);
        Assert.Empty(vm.SelectedItem!.SeedlingInfos[0].Replants!);
        Assert.Equal(new DateTime(2026, 4, 1), vm.ReplantsDate);
        Assert.True(vm.ReplantsSeedlingCommand.CanExecute(null));
        _dialog.Received(1).Warning("Ошибка пересадки", "Пересадка рассады");
    }

    [Fact]
    public void MarkDead_SelectsLastAliveSproutsAndPassesReason()
    {
        var batch = Batch();
        for (var number = 1; number <= 3; number++) batch.SeedlingInfos.Add(new() { Id = number + 1, SeedlingNumber = number, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch); vm.DeadNumbers = 2; vm.DeathNote = "Засохли";
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingChangeResultFromViewModel(batch.Id,
            [new(1, 0, Option.None<DateTime>(), Option.None<bool>()),
             new(2, 1, Option.Some(DateTime.Today), Option.None<bool>()),
             new(3, 2, Option.Some(DateTime.Today), Option.Some(true)) { DeathNote = Option.Some("Засохли") },
             new(4, 3, Option.Some(DateTime.Today), Option.Some(true)) { DeathNote = Option.Some("Засохли") }])));
        vm.DeathSeedlingCommand.Execute(null);
        _service.Received(1).ChangeAsync(Arg.Is<ChangeSeedlingFromViewModel>(request => request.Action == new SeedlingAction.MarkDead(2, Option.Some("Засохли"))));
        Assert.Equal(0, vm.DeadNumbers); Assert.Equal(string.Empty, vm.DeathNote);
        Assert.Equal(new[] { 2, 3 }, vm.SelectedItem!.SeedlingInfos.Where(info => info.IsDead == true).Select(info => info.SeedlingNumber));
    }

    [Fact]
    public void MarkDead_SaveFailure_PreservesInputAndRecords()
    {
        var batch = Batch(); batch.SeedlingInfos.Add(new() { Id = 2, SeedlingNumber = 1, GerminationDate = DateTime.Today });
        var vm = CreateVm(batch); vm.DeadNumbers = 1; vm.DeathNote = "Причина";
        _service.ChangeAsync(Arg.Any<ChangeSeedlingFromViewModel>()).Returns(Result.Failure<SeedlingChangeResultFromViewModel>("Ошибка гибели"));
        vm.DeathSeedlingCommand.Execute(null);
        Assert.Equal(1, vm.DeadNumbers);
        Assert.Equal("Причина", vm.DeathNote);
        Assert.Null(vm.SelectedItem!.SeedlingInfos[1].IsDead);
        _dialog.Received(1).Warning("Ошибка гибели", "Гибель рассады");
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

    [Fact]
    public void Edit_Cancel_RestoresDraftWithoutChangingOriginal()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.EditedItem!.Quantity = 7;
        vm.CancelUpdateSeedlingInfoCommand.Execute(null);
        Assert.Equal(3, vm.SelectedItem!.Quantity);
        Assert.Equal(3, vm.EditedItem!.Quantity);
    }

    [Fact]
    public void Edit_SaveFailure_PreservesOriginalAndDraft()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.EditedItem!.Quantity = 7;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _service.EditAsync(Arg.Any<EditSeedlingFromViewModel>()).Returns(Result.Failure<SeedlingEditResultFromViewModel>("Ошибка сохранения"));
        vm.UpdateSeedlingInfoCommand.Execute(null);
        Assert.Equal(3, vm.SelectedItem!.Quantity);
        Assert.Equal(7, vm.EditedItem!.Quantity);
        _dialog.Received(1).Warning("Ошибка сохранения", "Редактирование рассады");
    }

    [Fact]
    public void Edit_LandingDate_KeepsMetadataAndDisplayedDateConsistent()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        var date = new DateTime(2026, 4, 1);
        vm.EditedItem!.LandingDate = date;
        var details = SeedlingDetails.Create(vm.EditedItem.Values with { LunarPhase = "Фаза" }).ValueOrThrow();
        _service.EditAsync(Arg.Any<EditSeedlingFromViewModel>()).Returns(Result.Success(new SeedlingEditResultFromViewModel(details,
            new(batch.Id, [new(1, 0, Option.None<DateTime>(), Option.None<bool>())]))));
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        vm.UpdateSeedlingInfoCommand.Execute(null);
        Assert.Equal(date, vm.SelectedItem!.LandingDate);
        Assert.Equal(date, vm.SelectedItem!.SeedlingInfos[0].LandingDate);
    }

    [Fact]
    public void Edit_ConfirmationDeclined_PreservesDraftAndDoesNotWrite()
    {
        var batch = Batch(); var vm = CreateVm(batch);
        vm.EditedItem!.Quantity = 7;
        vm.UpdateSeedlingInfoCommand.Execute(null);
        Assert.Equal(3, vm.SelectedItem!.Quantity);
        Assert.Equal(7, vm.EditedItem.Quantity);
        _service.DidNotReceive().EditAsync(Arg.Any<EditSeedlingFromViewModel>());
    }
}
