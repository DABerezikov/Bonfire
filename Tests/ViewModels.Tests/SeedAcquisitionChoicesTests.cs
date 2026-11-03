using Bonfire.Application.Seeds;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;

namespace ViewModels.Tests;

public class SeedAcquisitionChoicesTests
{
    [Fact]
    public void DuplicateCultureNamesKeepSortAndProducerLinksFromEveryMatchingId()
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray<SeedReadFromViewModel>.Empty));
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])
        {
            Catalog = Option.Some(new SeedCatalogFromViewModel([new(1, "Малопа", "Цветы"), new(2, "Малопа", "Цветы")],
                [new(1, "Первая", 1, [1]), new(2, "Вторая", 2, [2])], [new(1, "Фирма"), new(2, "Фирма")]))
        }));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null); vm.AddCulture = "Малопа";
        Assert.Equal(2, vm.SortListView.Cast<object>().Count());
        Assert.Equal(2, vm.ProducerListView.Cast<object>().Count());
        vm.AddProducer = "Фирма";
        vm.AddSort = "Вторая";
        Assert.Equal("Фирма", vm.AddProducer);
        Assert.Equal("Фирма", Assert.Single(vm.ProducerListView.Cast<ProducerFromViewModel>()).Name);
    }

    [Fact]
    public async Task RefreshCatalogKeepsUnchangedChoiceInstances()
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray<SeedReadFromViewModel>.Empty));
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])
        { Catalog = Option.Some(new SeedCatalogFromViewModel([new(1, "Томат", "Овощи")], [new(1, "Черри", 1, [1])], [new(1, "Фирма")])) }));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null);
        var culture = vm.AddCultureList[0]; var sort = vm.AddSortList[0]; var producer = vm.AddProducerList[0];
        Assert.True((await vm.RefreshLibrariesAsync()).IsSuccess);
        Assert.Same(culture, vm.AddCultureList[0]); Assert.Same(sort, vm.AddSortList[0]); Assert.Same(producer, vm.AddProducerList[0]);
    }

    [Fact]
    public void SameSortNameAllowsAllLinkedProducers_WithoutFilteringAncestors()
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray<SeedReadFromViewModel>.Empty));
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])
        {
            Catalog = Option.Some(new SeedCatalogFromViewModel([new(1, "Томат", "Овощи")],
                [new(1, "Черри", 1, [1]), new(2, "Черри", 2, [1])], [new(1, "Первая"), new(2, "Вторая")]))
        }));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null); vm.AddCulture = "Томат"; vm.AddSort = "Черри";
        Assert.Equal(2, vm.ProducerListView.Cast<object>().Count());
        vm.AddProducer = "Вторая";
        Assert.Equal("Овощи", vm.AddClass); Assert.Equal("Томат", vm.AddCulture); Assert.Equal("Черри", vm.AddSort);
    }

    [Fact]
    public void CatalogFiltersDescendants_AndRetainsAllLibrariesWithoutStock()
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray<SeedReadFromViewModel>.Empty));
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])
        {
            Catalog = Option.Some(new SeedCatalogFromViewModel(
                [new(1, "Томат", "Овощи"), new(2, "Астра", "Цветы")],
                [new(1, "Черри", 1, [1]), new(2, "Белая", 2, [2])],
                [new(1, "Фирма"), new(2, "Цветочная")]))
        }));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null);
        Assert.Empty(vm.Seeds!); Assert.Equal(2, vm.AddCultureList.Count);
        vm.AddClass = "Овощи";
        Assert.Equal("Томат", Assert.Single(vm.CultureListView!.Cast<CultureFromViewModel>()).Name);
        Assert.Equal("Черри", Assert.Single(vm.SortListView.Cast<SortFromSeedsViewModel>()).Name);
        Assert.Equal("Фирма", Assert.Single(vm.ProducerListView.Cast<ProducerFromViewModel>()).Name);
        vm.AddCulture = "Астра";
        Assert.Equal("Цветы", vm.AddClass);
        vm.AddSort = "Белая"; vm.AddProducer = "Цветочная";
        vm.AddClass = "Овощи";
        Assert.Empty(vm.AddCulture); Assert.Empty(vm.AddSort); Assert.Empty(vm.AddProducer);
        Assert.NotEmpty(vm.AcquisitionSelectionHint);
        vm.AddCulture = "Новая культура";
        Assert.Equal(2, vm.ProducerListView.Cast<object>().Count());
        Assert.Equal("Овощи", vm.AddClass);
        vm.AddCulture = "Томат"; vm.AddSort = "Новый сорт";
        Assert.Equal(2, vm.ProducerListView.Cast<object>().Count());
        vm.AddProducer = "Цветочная";
        Assert.Equal("Томат", vm.AddCulture); Assert.Equal("Новый сорт", vm.AddSort); Assert.Equal("Овощи", vm.AddClass);
    }
}
