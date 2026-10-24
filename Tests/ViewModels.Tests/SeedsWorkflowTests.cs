using System.Threading.Tasks;

namespace ViewModels.Tests;

public class SeedsWorkflowTests
{
    private readonly ISeedsService _service = Substitute.For<ISeedsService>();
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();
    private readonly IReportService _reports = Substitute.For<IReportService>();

    private SeedsViewModel CreateVm(params Seed[] seeds)
    {
        _service.GetAllSeedsAsync().Returns(seeds);
        _service.GetEditorOptionsAsync().Returns(new SeedEditorOptions(["Красный"], ["Ранний"], seeds.Select(s => s.Plant.PlantSort.Producer).ToArray()));
        var vm = new SeedsViewModel(_service, _dialog, _reports);
        vm.LoadDataCommand.Execute(null);
        return vm;
    }

    internal static Seed Seed(int id, string culture = "Томат", string sort = "Черри", string plantClass = "Овощи", double quantity = 10) => new()
    {
        Id = id,
        Plant = new Plant
        {
            Id = id,
            PlantCulture = new PlantCulture { Id = id, Name = culture, Class = plantClass },
            PlantSort = new PlantSort { Id = id, Name = sort, Producer = new Producer { Id = id, Name = "Фирма " + id } }
        },
        SeedsInfo = new SeedsInfo { Id = id, AmountSeeds = quantity, ExpirationDate = new DateTime(DateTime.Now.Year + 1, 12, 31) }
    };

    [Fact]
    public void LoadData_PopulatesDistinctSuggestionsAndHidesEmptyInventory()
    {
        var vm = CreateVm(Seed(1), Seed(2), Seed(3, "Астра", "Белая", "Цветы", 0));
        Assert.Equal(3, vm.Seeds!.Count);
        Assert.Equal(2, vm.AddCultureList.Count);
        Assert.Equal(2, vm.AddSortList.Count);
        Assert.Equal(3, vm.AddProducerList.Count);
        Assert.Equal(2, vm.SeedsView!.Cast<SeedsFromViewModel>().Count());
        vm.IsHaving = false;
        Assert.Equal(3, vm.SeedsView!.Cast<SeedsFromViewModel>().Count());
    }

    [Fact]
    public void Filters_SelectCultureAndClassWithoutMutatingStoredCollection()
    {
        var vm = CreateVm(Seed(1), Seed(2, "Астра", "Белая", "Цветы"));
        vm.SeedFilter = "аст";
        Assert.Equal("Астра", Assert.Single(vm.SeedsView!.Cast<SeedsFromViewModel>()).Culture);
        vm.SeedFilter = "-Выбрать все-";
        vm.SeedsChoiceClassCommand.Execute("Овощи");
        Assert.Equal("Томат", Assert.Single(vm.SeedsView!.Cast<SeedsFromViewModel>()).Culture);
        Assert.Equal(2, vm.Seeds!.Count);
        vm.SeedsChoiceClassCommand.Execute("Выбрать все");
        Assert.Equal(2, vm.SeedsView!.Cast<SeedsFromViewModel>().Count());
    }

    [Fact]
    public void Suggestions_UseCaseInsensitiveSubstringSearch()
    {
        var vm = CreateVm(Seed(1), Seed(2, "Астра", "Белая", "Цветы"));
        vm.AddCulture = "АСТ"; vm.AddSort = "бел"; vm.AddProducer = "2";
        Assert.Equal("Астра", Assert.Single(vm.CultureListView!.Cast<CultureFromViewModel>()).Name);
        Assert.Equal("Белая", Assert.Single(vm.SortListView!.Cast<SortFromSeedsViewModel>()).Name);
        Assert.Equal("Фирма 2", Assert.Single(vm.ProducerListView.Cast<ProducerFromViewModel>()).Name);
    }

    [Fact]
    public void SourceFlags_AreExclusiveAndCollectedSeedsUseOwnProducer()
    {
        var vm = CreateVm();
        vm.IsSold = true; Assert.True(vm.IsSold); Assert.False(vm.IsDonated);
        vm.IsDonated = true; Assert.False(vm.IsSold); Assert.True(vm.IsDonated);
        vm.IsCollected = true;
        Assert.False(vm.IsDonated); Assert.True(vm.IsCollected);
        Assert.Equal(Producers.Own, vm.AddProducer);
        vm.IsCollected = false;
        Assert.True(vm.IsCollected); // WPF-снятие старой radio-кнопки не меняет выбранный источник.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddOrCorrect_PassesParsedRequestAndOnlyAppendsNewRecords(bool isNew)
    {
        var existing = Seed(1);
        var vm = CreateVm(existing);
        var returned = isNew ? Seed(2, "Астра", "Новая", "Цветы") : existing;
        _service.AddOrUpdateSeedAsync(Arg.Any<AddSeedRequest>(), Arg.Any<IReadOnlyList<Seed>>()).Returns((returned, isNew));
        vm.SeedSource = "Куплено"; vm.AddClass = "Цветы"; vm.AddCulture = "Астра";
        vm.AddSort = "Новая"; vm.AddProducer = "Фирма"; vm.AddSize = Units.GramsOption;
        vm.AddQuantityInPac = "0,5"; vm.AddQuantityPac = "2"; vm.AddCostPack = "10,50";
        vm.AddNote = "Заметка";
        vm.AddOrCorrectSeedCommand.Execute(null);
        Assert.Equal(isNew ? 2 : 1, vm.Seeds!.Count);
        _service.Received(1).AddOrUpdateSeedAsync(Arg.Is<AddSeedRequest>(r => r.QuantityInPack == 0.5 && r.PackCount == 2 && r.CostPack == 10.5m && r.Note == "Заметка"), Arg.Any<IReadOnlyList<Seed>>());
        Assert.Equal(string.Empty, vm.AddCulture);
        Assert.Equal(string.Empty, vm.AddQuantityInPac);
    }

    [Fact]
    public void SaveConfirmed_UsesDetachedDraftAndReloadsSharedData()
    {
        var original = Seed(1);
        var vm = CreateVm(original);
        vm.SelectedItem = original;
        vm.EditedItem.SeedsInfo.Note = "Черновик";
        vm.SeedSortDetail!.Description = "Общее описание";
        vm.EditedProducerId = 2;
        var saved = Seed(1); saved.SeedsInfo.Note = "Черновик";
        saved.Plant.PlantSort.Description = "Общее описание";
        saved.Plant.PlantSort.Producer = new Producer { Id = 2, Name = "Новый" };
        _service.GetAllSeedsAsync().Returns(new[] { saved });
        _service.SaveSeedEditAsync(Arg.Any<Seed>(), Arg.Any<SortEditModel>(), 2).Returns(_ =>
        {
            Assert.Null(original.SeedsInfo.Note);
            Assert.Null(original.Plant.PlantSort.Description);
            return Task.CompletedTask;
        });
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        vm.UpdateSeedsInfoCommand.Execute(null);
        Assert.Equal("Черновик", vm.SelectedItem!.SeedsInfo.Note);
        Assert.Equal("Общее описание", vm.SeedSortDetail!.Description);
        Assert.Equal(2, vm.EditedProducerId);
        Assert.False(vm.HasUnsavedSeedChanges);
        Assert.True(vm.CanEditSeedDetails);
        _dialog.Received().YesNoQuestion(Arg.Is<string>(s => s.Contains("всех записей")), "Редактирование семян");
    }

    [Fact]
    public void SaveAndDelete_BindingErrorBlocksSaveAndCancelDeletionKeepsDraft()
    {
        var vm = CreateVm(Seed(1)); vm.SelectedItem = vm.Seeds![0];
        vm.EditorHasBindingErrors = true;
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        vm.EditorHasBindingErrors = false; vm.EditedItem.SeedsInfo.AmountSeeds = -1;
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        vm.EditedItem.SeedsInfo.Note = "Не терять";
        _dialog.RequestDeleteReason(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);
        vm.DeleteSeedCommand.Execute(null);
        Assert.Single(vm.Seeds);
        Assert.Equal("Не терять", vm.EditedItem.SeedsInfo.Note);
        _service.DidNotReceive().DeleteSeedWithReasonAsync(Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public void Reports_AllAndFilteredReceiveCorrectRecords()
    {
        var vm = CreateVm(Seed(1), Seed(2, "Астра", "Белая", "Цветы"));
        vm.IsActive = true;
        vm.CreateSeedsReportCommand.Execute(null);
        _reports.Received().CreateSeedsReport(Arg.Is<IEnumerable<SeedReportItem>>(rows => rows.Count() == 2), "Семена");
        vm.SeedFilter = "Астра";
        vm.CreateFilteredSeedsReportCommand.Execute(null);
        _reports.Received().CreateSeedsReport(Arg.Is<IEnumerable<SeedReportItem>>(rows => rows.Single().Culture == "Астра"), "Семена_выборка");
    }
}
