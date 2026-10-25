using System.Windows.Input;

using Bonfire.Domain.Functional;
using Bonfire.Application.Libraries;

namespace ViewModels.Tests;

public class LibraryWorkflowTests
{
    private readonly ILibraryService _library = Substitute.For<ILibraryService>();
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();
    private readonly Seed _seed = SeedsWorkflowTests.Seed(1);

    private (LibraryEditorViewModel Vm, Action Change, ICommand Save, ICommand Cancel, Func<bool> Dirty) Setup(string kind)
    {
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), _dialog, Substitute.For<IReportService>())
        {
            Seeds = [_seed],
            AddSortList = [new() { Id = 1, Name = "Черри" }],
            AddCultureList = [new() { Id = 1, Name = "Томат" }],
            AddProducerList = [new() { Id = 1, Name = "Фирма 1" }]
        };
        var sort = _seed.Plant.PlantSort; sort.Plants.Add(_seed.Plant);
        _library.GetSortAsync(1).Returns(Result.Success(sort));
        _library.GetCultureAsync(1).Returns(Result.Success(_seed.Plant.PlantCulture));
        _library.GetProducerAsync(1).Returns(Result.Success(sort.Producer));
        var vm = new LibraryEditorViewModel(_library, _dialog, seeds);
        switch (kind)
        {
            case "Sort":
                vm.SelectedSort = seeds.AddSortList[0];
                return (vm, () => vm.SortDetail!.Name = "Новое", vm.SaveSortCommand, vm.CancelSortCommand, () => vm.SortDetail!.IsDirty);
            case "Culture":
                vm.SelectedCulture = seeds.AddCultureList[0];
                return (vm, () => vm.CultureDetail!.Name = "Новое", vm.SaveCultureCommand, vm.CancelCultureCommand, () => vm.CultureDetail!.IsDirty);
            default:
                vm.SelectedProducer = seeds.AddProducerList[0];
                return (vm, () => vm.ProducerDetailName = "Новое", vm.SaveProducerCommand, vm.CancelProducerCommand, () => vm.ProducerDetailIsDirty);
        }
    }

    [Theory]
    [InlineData("Sort")]
    [InlineData("Culture")]
    [InlineData("Producer")]
    public void Save_Success_UpdatesSharedSeedCacheAndClearsDirtyFlag(string kind)
    {
        var setup = Setup(kind);
        _library.UpdateSortAsync(Arg.Any<SortEditModel>()).Returns(c =>
        {
            var sort = new SortDetailsFromViewModel(1, c.Arg<SortEditModel>().Name!, []);
            return Result.Success(sort);
        });
        _library.UpdateCultureAsync(Arg.Any<CultureEditModel>()).Returns(c => Result.Success(new PlantCulture { Id = 1, Name = c.Arg<CultureEditModel>().Name!, Class = "Овощи" }));
        _library.UpdateProducerAsync(1, "Новое").Returns(Result.Success(new Producer { Id = 1, Name = "Новое" }));
        setup.Change(); setup.Save.Execute(null);
        Assert.False(setup.Dirty());
        Assert.Equal("Новое", kind switch
        {
            "Sort" => _seed.Plant.PlantSort.Name,
            "Culture" => _seed.Plant.PlantCulture.Name,
            _ => _seed.Plant.PlantSort.Producer.Name
        });
        _dialog.Received(1).Information("Сохранено", Arg.Any<string>());
    }

    [Fact]
    public void SaveSort_SavedCultureLinks_UpdatesMatchingSeedAndLibraryList()
    {
        var setup = Setup("Sort");
        _library.UpdateSortAsync(Arg.Any<SortEditModel>()).Returns(Result.Success(
            new SortDetailsFromViewModel(1, "Новое",
                [new(_seed.Plant.Id, new(7, "Астра", "Цветы")), new(999, new(8, "Другая", "Зелень"))])));
        setup.Change();
        setup.Save.Execute(null);
        Assert.Equal(7, _seed.Plant.PlantCulture.Id);
        Assert.Equal("Астра", _seed.Plant.PlantCulture.Name);
        Assert.Equal("Цветы", _seed.Plant.PlantCulture.Class);
        Assert.Equal("Новое", setup.Vm.Sort.Single().Name);
        Assert.False(setup.Dirty());
    }

    [Theory]
    [InlineData("Sort")]
    [InlineData("Culture")]
    [InlineData("Producer")]
    public void Save_Failure_KeepsDirtyDraftAndShowsError(string kind)
    {
        var setup = Setup(kind);
        _library.UpdateSortAsync(Arg.Any<SortEditModel>()).Returns(Result.Failure<SortDetailsFromViewModel>("Конфликт"));
        _library.UpdateCultureAsync(Arg.Any<CultureEditModel>()).Returns(Result.Failure<PlantCulture>("Конфликт"));
        _library.UpdateProducerAsync(1, "Новое").Returns(Result.Failure<Producer>("Конфликт"));
        setup.Change(); setup.Save.Execute(null);
        Assert.True(setup.Dirty());
        Assert.Equal("Черри", _seed.Plant.PlantSort.Name);
        _dialog.Received(1).Error("Конфликт", Arg.Any<string>());
        _dialog.DidNotReceive().Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("Sort")]
    [InlineData("Culture")]
    [InlineData("Producer")]
    public void Cancel_ReloadsStoredValuesAndClearsDirtyFlag(string kind)
    {
        var setup = Setup(kind);
        setup.Change(); setup.Cancel.Execute(null);
        Assert.False(setup.Dirty());
        Assert.Equal(kind switch { "Sort" => "Черри", "Culture" => "Томат", _ => "Фирма 1" },
            kind switch { "Sort" => setup.Vm.SortDetail!.Name, "Culture" => setup.Vm.CultureDetail!.Name, _ => setup.Vm.ProducerDetailName });
    }
}
