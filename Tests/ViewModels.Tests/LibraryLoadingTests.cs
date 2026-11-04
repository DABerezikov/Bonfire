using Bonfire.Application.Libraries;
using BonfireDB.Libraries;
using Bonfire.Domain.Functional;

namespace ViewModels.Tests;

public class LibraryLoadingTests
{
    [Fact]
    public void LoadCulture_SelectionChanges_IgnoresStaleDetail()
    {
        var service = Substitute.For<ILibraryService>();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        var pending = new TaskCompletionSource<Result<CultureDetailsFromViewModel>>();
        service.GetCultureAsync(1).Returns(pending.Task);
        service.GetCultureAsync(2).Returns(Result.Success(new CultureDetailsFromViewModel(2, "Новая", "Овощи")));
        var vm = new LibraryEditorViewModel(service, Substitute.For<IUserDialog>(), seeds, new PlanningLibrariesViewModel(Substitute.For<IPlanningService>()), Substitute.For<Bonfire.Application.Libraries.IManageSortOptions>());
        vm.SelectedCulture = new() { Id = 1 }; vm.SelectedCulture = new() { Id = 2 };
        var draft = vm.CultureDetail!; draft.Name = "Черновик";
        pending.SetResult(Result.Success(new CultureDetailsFromViewModel(1, "Старая", "Цветы")));
        Assert.Same(draft, vm.CultureDetail); Assert.Equal("Черновик", draft.Name);
    }
}
