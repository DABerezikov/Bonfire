using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
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
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel(["Красный"], ["Ранний"], seeds.Select(s => LibraryReadDbMapper.Read(s.Plant.PlantSort.Producer)).ToImmutableArray())));
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
    public void SeedEditor_PlantingSchemeRequiresBothFieldsAndClearsPairedErrors() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        TextBox Field(string name) => WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail." + name);
        var spacing = Field("LandingPattern");
        var rows = Field("LandingRowSpacing");
        void Set(TextBox box, string text)
        {
            box.Text = text;
            box.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            WpfRuntime.Layout(editor, 1060, 500);
        }
        Set(spacing, "20");
        Assert.True(Validation.GetHasError(spacing));
        Assert.True(Validation.GetHasError(rows));
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        Set(rows, "30");
        Assert.False(Validation.GetHasError(spacing));
        Assert.False(Validation.GetHasError(rows));
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
        Set(spacing, "");
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        Set(rows, "");
        Assert.False(Validation.GetHasError(spacing));
        Assert.False(Validation.GetHasError(rows));
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
    });

    [Fact]
    public void SeedEditor_ErrorTooltipShowsReasonAndDisappearsAfterCorrection() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.YieldKg");
        foreach (var (text, message) in new[]
        {
            ("2,555555", "Допустим один знак после запятой"),
            ("2б", "Введите число, например 2,6")
        })
        {
            input.Text = text;
            input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            WpfRuntime.Layout(editor, 1060, 500);
            Assert.Equal(message, input.ToolTip);
        }
        input.Text = "2,5";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        WpfRuntime.Layout(editor, 1060, 500);
        Assert.False(Validation.GetHasError(input));
        Assert.Null(input.ToolTip);
    });

    [Fact]
    public void SeedEditor_YieldInvalidTextShowsValidationWithoutBindingConversionError() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.YieldKg");
        var source = System.Diagnostics.PresentationTraceSources.DataBindingSource;
        var level = source.Switch.Level;
        using var log = new System.IO.StringWriter();
        using var listener = new System.Diagnostics.TextWriterTraceListener(log);
        source.Listeners.Add(listener);
        source.Switch.Level = System.Diagnostics.SourceLevels.Error;
        try
        {
            input.Text = "2б";
            input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Assert.Equal("2б", input.Text);
            Assert.True(Validation.GetHasError(input));
            Assert.Equal("Введите число, например 2,6", Assert.Single(Validation.GetErrors(input)).ErrorContent);
            Assert.True(vm.EditorHasBindingErrors);
            Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
            Assert.Null(vm.SeedSortDetail!.YieldKg);
            Assert.Equal("", log.ToString());
            input.Text = "2,6";
            input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Assert.False(Validation.GetHasError(input));
            Assert.False(vm.EditorHasBindingErrors);
            Assert.Equal(2.6, vm.SeedSortDetail.YieldKg);
            Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
        }
        finally
        {
            source.Listeners.Remove(listener);
            source.Switch.Level = level;
        }
    });

    [Fact]
    public void SeedEditor_YieldTypingPreservesCommaUntilCommit() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.YieldKg");
        var original = vm.SeedSortDetail!.YieldKg;
        foreach (var text in new[] { "2", "2,", "2,5" })
        {
            input.Text = text;
            WpfRuntime.Layout(editor, 1060, 500);
            Assert.Equal(text, input.Text);
            Assert.Equal(original, vm.SeedSortDetail.YieldKg);
        }
        input.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
        Assert.Equal(2.5, vm.SeedSortDetail.YieldKg);
        Assert.False(Validation.GetHasError(input));
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
    });

    [Fact]
    public void SeedEditor_YieldAcceptsCommaAndBlocksExtraDecimalPlaces() => runtime.Invoke(() =>
    {
        var vm = SeedsVm(null, SeedsWorkflowTests.Seed(1));
        var editor = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(editor, 1060, 500);
        var input = WpfRuntime.Descendants<TextBox>(editor).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.YieldKg");
        input.Text = "2,5";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.Equal(2.5, vm.SeedSortDetail!.YieldKg);
        Assert.False(Validation.GetHasError(input));
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
        input.Text = "2,55";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.True(Validation.GetHasError(input));
        Assert.False(vm.UpdateSeedsInfoCommand.CanExecute(null));
        input.Text = "0,1";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.Equal(0.1, vm.SeedSortDetail.YieldKg);
        Assert.False(Validation.GetHasError(input));
        Assert.True(vm.UpdateSeedsInfoCommand.CanExecute(null));
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
