using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace ViewModels.Tests;

public class SortOptionsWorkflowTests
{
    [Fact]
    public void FailedRenameOrDeletePreservesDraft_AndRequiredRuleControlsSave()
    {
        var options = Substitute.For<IManageSortOptions>(); var dialog = Substitute.For<IUserDialog>();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), dialog, Substitute.For<IReportService>());
        var vm = new SortOptionsViewModel(options, dialog, seeds);
        Assert.True(vm.NameRule.IsRequired); Assert.EndsWith("*", vm.NameRule.Caption);
        Assert.False(vm.SaveCommand.CanExecute(null)); Assert.NotEmpty(vm[nameof(vm.Name)]);
        vm.Selected = new(SortOptionCategory.Color, "Красный", true); vm.Name = "Алый";
        options.SaveAsync(SortOptionCategory.Color, Arg.Any<Option<string>>(), "Алый").Returns(Result.Failure<SortOptionFromViewModel>("Ошибка записи"));
        vm.SaveCommand.Execute(null);
        Assert.Equal("Алый", vm.Name); Assert.Equal("Красный", vm.Selected!.Name); Assert.False(vm.IsBusy);
        dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        options.DeleteAsync(SortOptionCategory.Color, "Красный").Returns(Result.Failure<bool>("Используется"));
        vm.DeleteCommand.Execute(null);
        Assert.Equal("Алый", vm.Name); Assert.Equal("Красный", vm.Selected!.Name); Assert.Equal("Используется", vm.Status);
    }

    [Fact]
    public void FailedRefreshAfterSavedRenamePreservesInput_AndDoesNotReportCompleteSuccess()
    {
        var options = Substitute.For<IManageSortOptions>(); var dialog = Substitute.For<IUserDialog>();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), dialog, Substitute.For<IReportService>());
        var vm = new SortOptionsViewModel(options, dialog, seeds);
        vm.Selected = new(SortOptionCategory.Color, "Красный", true); vm.Name = "Алый";
        options.SaveAsync(SortOptionCategory.Color, Arg.Any<Option<string>>(), "Алый").Returns(Result.Success(new SortOptionFromViewModel(SortOptionCategory.Color, "Алый", true)));
        options.ReadAsync().Returns(Result.Failure<System.Collections.Immutable.ImmutableArray<SortOptionFromViewModel>>("Чтение недоступно"));
        vm.SaveCommand.Execute(null);
        Assert.Equal("Алый", vm.Name); Assert.Equal("Красный", vm.Selected!.Name);
        Assert.Contains("Чтение недоступно", vm.Status); Assert.False(vm.IsBusy);
    }
}
