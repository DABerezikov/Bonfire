using BonfireDB.Libraries;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Bonfire.Views;
using Bonfire.Views.GardenPlan;
using MoonCalendar;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class WpfBehaviorTests(WpfRuntime runtime)
{
    private static SeedsViewModel SeedsVm(IUserDialog? dialog = null, params Seed[] seeds)
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(seeds.ReadSeeds());
        service.GetEditorOptionsAsync().Returns(new SeedEditorOptions(["Красный"], ["Ранний"], seeds.Select(s => LibraryReadDbMapper.Read(s.Plant.PlantSort.Producer)).ToArray()));
        var vm = new SeedsViewModel(service, dialog ?? Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null);
        if (seeds.Length > 0) vm.SelectedItem = vm.Seeds![0];
        return vm;
    }

    [Theory]
    [InlineData("Seeds")]
    [InlineData("Seedlings")]
    [InlineData("Library")]
    [InlineData("Garden")]
    [InlineData("Grid")]
    public void Screens_ConstructAndLayoutWithRealResources(string screen) => runtime.Invoke(() =>
    {
        var seeds = SeedsVm();
        var seedlingService = Substitute.For<ISeedlingsService>();
        seedlingService.ReadSeedlingsAsync().Returns(Array.Empty<Seedling>().ReadSeedlings());
        seedlingService.Lunar.Returns(new MoonPhase());
        var seedService = Substitute.For<ISeedsService>(); seedService.ReadSeedsAsync().Returns(Array.Empty<Seed>().ReadSeeds());
        var gardenService = Substitute.For<IGardenService>(); gardenService.ReadPlansAsync().Returns(Bonfire.Domain.Functional.Result.Success(System.Collections.Immutable.ImmutableArray<Bonfire.Application.Garden.GardenPlanResultFromViewModel>.Empty));
        var garden = new GardenPlanViewModel(gardenService, Substitute.For<IUserDialog>(), seedlingService, seedService, Substitute.For<IPlantingService>());
        FrameworkElement view = screen switch
        {
            "Seeds" => new SeedsWindow { DataContext = seeds },
            "Seedlings" => new SeedlingsWindow { DataContext = new SeedlingsViewModel(seedlingService, seedService, Substitute.For<IUserDialog>(), Substitute.For<IReportService>()) },
            "Library" => new LibraryEditorWindow { DataContext = new LibraryEditorViewModel(Substitute.For<ILibraryService>(), Substitute.For<IUserDialog>(), seeds) },
            "Garden" => new GardenPlanView { DataContext = garden },
            _ => new PlantingGridView { DataContext = garden }
        };
        WpfRuntime.Layout(view);
        Assert.True(view.ActualWidth > 0);
        Assert.True(view.ActualHeight > 0);
    });

    [Fact]
    public void SeedEditor_InvalidNumericInputBlocksSaveAndCorrectionRestoresIt() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box => BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.PlantHeight");
        input.Text = "не число"; input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.True(Validation.GetHasError(input));
        Assert.True(vm.EditorHasBindingErrors);
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        input.Text = "30"; input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.False(Validation.GetHasError(input));
        Assert.False(vm.EditorHasBindingErrors);
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
    });

    [Fact]
    public void SeedEditor_PackageBinding_UpdatesDraftAndReflectsModelNotifications() => runtime.Invoke(() =>
    {
        var source = SeedsWorkflowTests.Seed(1);
        var vm = SeedsVm(null, source);
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "EditedItem.Package.AmountSeeds");
        input.Text = "8";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.Equal(8, vm.EditedItem!.Package.AmountSeeds);
        Assert.Equal(10, source.SeedsInfo.AmountSeeds);
        Assert.True(vm.HasUnsavedSeedChanges);
        vm.EditedItem.Package.AmountSeeds = 7;
        WpfRuntime.Layout(editor, 1060, 500);
        Assert.Equal("7", input.Text);
    });

    [Fact]
    public void SeedEditor_HeaderAndInputsPreserveAgreedPresentation() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var header = WpfRuntime.Descendants<TextBlock>(editor).Single(t => t.Text == "Томат - Черри");
        Assert.Equal(TextAlignment.Center, header.TextAlignment);
        var inputs = WpfRuntime.Descendants<TextBox>(editor).Where(box => BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path.StartsWith("SeedSortDetail.") == true && !box.AcceptsReturn);
        Assert.NotEmpty(inputs);
        Assert.All(inputs, box => Assert.Equal(TextAlignment.Center, box.TextAlignment));
        Assert.All(WpfRuntime.Descendants<TextBox>(editor).Where(b => b.AcceptsReturn), box => Assert.Equal(TextAlignment.Left, box.TextAlignment));
        Assert.Equal(new[] { "Сохранить", "Удалить" }, WpfRuntime.Descendants<Button>(editor).Where(button => button.Command != null).Select(button => button.Content?.ToString()));
    });

    [Fact]
    public void SeedsRow_DoubleClickOpensEditorAndRefusedDiscardKeepsItOpen() => runtime.Invoke(() =>
    {
        var dialog = Substitute.For<IUserDialog>();
        dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var vm = SeedsVm(dialog, SeedsWorkflowTests.Seed(1));
        var view = new SeedsWindow { DataContext = vm };
        WpfRuntime.Layout(view);
        var grid = WpfRuntime.Descendants<DataGrid>(view).First();
        var row = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
        grid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent, Source = row });
        Assert.Equal(Visibility.Visible, row.DetailsVisibility);
        vm.EditedItem!.Package.Note = "Черновик";
        grid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent, Source = row });
        Assert.Equal(Visibility.Visible, row.DetailsVisibility);
        Assert.Equal("Черновик", vm.EditedItem!.Package.Note);
    });

    [Fact]
    public void SeedsScreen_DeletionOnlyExistsInsideRowEditor() => runtime.Invoke(() =>
    {
        var view = new SeedsWindow { DataContext = SeedsVm(null, SeedsWorkflowTests.Seed(1)) };
        WpfRuntime.Layout(view);
        Assert.DoesNotContain(WpfRuntime.Descendants<TextBlock>(view), text => text.Text == "Комментарий при удалении");
        Assert.DoesNotContain(WpfRuntime.Descendants<Button>(view), button => button.Content?.ToString() == "Удалить");
    });
}
