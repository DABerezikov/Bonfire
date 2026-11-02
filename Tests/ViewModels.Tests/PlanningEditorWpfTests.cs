using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Models.Planning;
using Bonfire.Views.Planning;
using Xunit.Abstractions;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class PlanningEditorWpfTests(WpfRuntime runtime, ITestOutputHelper output)
{
    [Fact]
    public void ShoppingExportButton_BindsToCommandAndRequiresSavedBatches() => runtime.Invoke(() =>
    {
        var vm = Model(); var view = new PlantingPlanningView { DataContext = vm }; WpfRuntime.Layout(view, 1280, 720);
        var button = Assert.IsType<Button>(view.FindName("PlanningShoppingExportButton"));
        Assert.Same(vm.ExportShoppingListCommand, button.Command);
        Assert.False(button.IsEnabled);
        Assert.Contains("сохранённым", button.ToolTip.ToString());
    });

    [Fact]
    public void StockFilter_RealSelectionRemainsInDraftWhileUnavailableChoicesAreHidden() => runtime.Invoke(() =>
    {
        var service = Service();
        var available = new PlanningChoiceFromViewModel(1, "Томат", 2, "Черри") { HasAvailableSeeds = true };
        var absent = new PlanningChoiceFromViewModel(3, "Арбуз", 4, "Алый сладкий");
        service.ReadPlansAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(System.Collections.Immutable.ImmutableArray<PlantingPlanSummaryFromViewModel>.Empty));
        service.ReadBedsAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(System.Collections.Immutable.ImmutableArray<PlanningBedFromViewModel>.Empty));
        service.ReadChoicesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(System.Collections.Immutable.ImmutableArray.Create(available, absent)));
        var vm = new Bonfire.ViewModels.PlantingPlanningViewModel(service, Substitute.For<IUserDialog>(), new(service), TimeProvider.System, Substitute.For<IReportService>());
        vm.LoadAsync().GetAwaiter().GetResult();
        var view = new PlantingPlanningView { DataContext = vm }; WpfRuntime.Layout(view, 1280, 720);
        var choices = WpfRuntime.Descendants<ComboBox>(view).Single(box => ReferenceEquals(box.ItemsSource, vm.Choices));
        Assert.Single(choices.Items);
        vm.ShowChoicesWithoutSeeds = true; WpfRuntime.Layout(view, 1280, 720);
        choices.SelectedItem = absent; WpfRuntime.Layout(view, 1280, 720);
        var values = vm.Draft.Values; var revision = vm.Draft.Revision;
        vm.ShowChoicesWithoutSeeds = false; WpfRuntime.Layout(view, 1280, 720);
        Assert.Same(absent, choices.SelectedItem); Assert.Same(absent, vm.Draft.Choice);
        Assert.Equal(values, vm.Draft.Values); Assert.Equal(revision, vm.Draft.Revision);
        Assert.Contains("нет в наличии", vm.ChoiceAvailabilityText);
        vm.NewBatch(); WpfRuntime.Layout(view, 1280, 720);
        Assert.Single(choices.Items); Assert.Null(vm.Draft.Choice);
    });

    [Fact]
    public void RequiredMarkerAndFieldValidation_ChangeTogetherInRealView() => runtime.Invoke(() =>
    {
        var vm = Model();
        vm.Draft.ManualSeedCountText = "";
        vm.Draft.Profile.ConfigureInheritance(new(), new(), Option.None<GerminationPercentage>(), Option.None<LossPercentage>());
        vm.EditorStep = 3;
        var view = new PlantingPlanningView { DataContext = vm }; WpfRuntime.Layout(view, 1280, 720);
        var percent = WpfRuntime.Descendants<TextBox>(view).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "GermPercentText");
        var label = WpfRuntime.Descendants<TextBlock>(view).Single(block =>
            BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path.Path == "GerminationPercentLabel");
        Assert.Equal(vm.Draft.Profile.GerminationPercentRule.Caption, label.Text);
        Assert.Contains("*", label.Text); Assert.True(Validation.GetHasError(percent));
        Assert.False(vm.NextStepCommand.CanExecute(null)); Assert.False(vm.ResultStepEnabled);
        vm.Draft.ManualSeedCountText = "30";
        WpfRuntime.Layout(view, 1280, 720);
        Assert.DoesNotContain("*", label.Text); Assert.False(Validation.GetHasError(percent));
    });

    [Fact]
    public void ChooseSowing_RealButtonSetsDateRecalculatesAndShowsPersistentConfirmation() => runtime.Invoke(() =>
    {
        var service = Service();
        var vm = new Bonfire.ViewModels.PlantingPlanningViewModel(service, Substitute.For<IUserDialog>(), new(service), TimeProvider.System, Substitute.For<IReportService>());
        vm.OpenPlanAsync(new(1, 1, "Сезон", 2027, "Томск")).GetAwaiter().GetResult();
        vm.Draft.Choice = new(1, "Томат", 2, "Черри");
        vm.Draft.TransplantStartText = "20.05.2027"; vm.Draft.TransplantEndText = "25.05.2027";
        vm.Draft.ManualSeedCountText = "30";
        var date = new DateOnly(2027, 3, 20);
        service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var preview = PlanningWorkflowTests.Preview(call.Arg<PlanningBatchInputFromViewModel>());
            return Result.Success(preview with { Document = preview.Document.Map(document => PlanningBatchDocument.Create(
                document.Batch, document.Settings, document.References,
                document.Preview with { Lunar = new SowingRecommendations.Available(
                    [new(date, LunarTrend.Growing, 0, true, document.Preview.Agronomy, Option.None<CandidateReadiness>())], true, false,
                    Option.None<ManualSowingAssessment>()) }
                , document.Quantity, document.CalculatedAt).ValueOrThrow()) });
        });
        vm.CalculateAsync().GetAwaiter().GetResult(); vm.EditorStep = 4;
        var view = new PlantingPlanningView { DataContext = vm }; WpfRuntime.Layout(view, 1280, 720);
        var button = WpfRuntime.Descendants<Button>(view).Single(x => x.CommandParameter is SowingCandidateItem);
        Assert.Same(vm.ChooseSowingCommand, button.Command);
        typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(button, null);
        WpfRuntime.Layout(view, 1280, 720);
        Assert.Equal("20.03.2027", vm.Draft.ChosenSowingText);
        Assert.Equal(PlannedDateOrigin.LunarRecommendation, vm.Draft.Values.SowingOrigin);
        Assert.NotNull(vm.Preview);
        Assert.Equal(Option.Some(date), vm.Preview.Input.Choices.SowingDate);
        Assert.Equal(4, vm.EditorStep);
        Assert.Contains("20.03.2027", Assert.IsType<TextBlock>(view.FindName("PlanningSelectedSowingText")).Text);
        Assert.True(vm.Candidates.Single().IsSelected);
        Assert.True(vm.SaveCommand.CanExecute(null));
        Assert.False(WpfRuntime.Descendants<Button>(view).Single(x => x.CommandParameter is SowingCandidateItem).IsEnabled);
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "selected-sowing-1280-144.png");
        Render(view, 1280, 720, 144, path); output.WriteLine(path);
    });

    [Fact]
    public void DatePickers_ValidateBothBoundsAndKeepInvalidText() => runtime.Invoke(() =>
    {
        var vm = Model(); vm.EditorStep = 1;
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        var start = Assert.IsType<PlanningDateInput>(view.FindName("PlanningStartInput"));
        var end = Assert.IsType<PlanningDateInput>(view.FindName("PlanningEndInput"));
        Assert.Contains("Начало", start.ToolTip.ToString());
        Assert.Contains("Окончание", end.ToolTip.ToString());
        Assert.IsType<DatePicker>(end.FindName("CalendarPicker")).SelectedDate = new DateTime(2027, 5, 19);
        WpfRuntime.Layout(view, 1280, 720);
        Assert.True(Validation.GetHasError(end));
        Assert.False(vm.NextStepCommand.CanExecute(null));
        Assert.Contains("позже", vm.StepGuidance);
        Assert.IsType<DatePicker>(start.FindName("CalendarPicker")).SelectedDate = new DateTime(2027, 5, 18);
        WpfRuntime.Layout(view, 1280, 720);
        Assert.False(Validation.GetHasError(end));
        Assert.True(vm.NextStepCommand.CanExecute(null));
        Assert.IsType<TextBox>(end.FindName("DateText")).Text = "не дата";
        WpfRuntime.Layout(view, 1280, 720);
        Assert.Equal("не дата", vm.Draft.TransplantEndText);
        Assert.True(Validation.GetHasError(end));
        Assert.Contains("Окончание", vm.Draft[nameof(vm.Draft.TransplantEndText)]);
    });

    [Theory]
    [InlineData(1280, 720, 144)]
    [InlineData(1920, 900, 96)]
    public void Wizard_AllStepsRenderAndUnavailableStepsStayLocked(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = Model();
        var view = new PlantingPlanningView { DataContext = vm };
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningWizardUi", Guid.NewGuid().ToString("N"), $"{width}-{dpi}");
        Directory.CreateDirectory(folder);
        output.WriteLine(folder);
        for (var step = 0; step < 5; step++)
        {
            if (step == 4) vm.CalculateAsync().GetAwaiter().GetResult();
            vm.EditorStep = step;
            WpfRuntime.Layout(view, width, height);
            Assert.Equal(step, vm.EditorStep);
            var tabs = Assert.IsType<TabControl>(view.FindName("PlanningSteps"));
            Assert.Equal(step, tabs.SelectedIndex);
            foreach (var picker in WpfRuntime.Descendants<PlanningDateInput>(view))
                Assert.Equal(BindingStatus.Active, BindingOperations.GetBindingExpression(picker, PlanningDateInput.TextProperty)!.Status);
            Render(view, width, height, dpi, Path.Combine(folder, $"step-{step + 1}.png"));
        }
        vm.EditorStep = 0;
        vm.Draft.Choice = null;
        WpfRuntime.Layout(view, width, height);
        var steps = Assert.IsType<TabControl>(view.FindName("PlanningSteps"));
        Assert.All(steps.Items.Cast<TabItem>().Skip(1), tab => Assert.False(tab.IsEnabled));
    });

    [Fact]
    public void InvalidText_RemainsInEditorAndBlocksCalculation() => runtime.Invoke(() =>
    {
        var vm = Model();
        vm.EditorStep = 2;
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        var target = Assert.IsType<TextBox>(view.FindName("PlanningTargetInput"));
        target.Text = "не число";
        WpfRuntime.Layout(view, 1280, 720);
        Assert.Equal("не число", vm.Draft.TargetPlantsText);
        Assert.True(Validation.GetHasError(target));
        Assert.False(vm.CalculateCommand.CanExecute(null));
        Assert.False(vm.SaveCommand.CanExecute(null));
        target.Text = "40";
        WpfRuntime.Layout(view, 1280, 720);
        Assert.False(Validation.GetHasError(target));
        Assert.True(vm.CalculateCommand.CanExecute(null));
    });

    [Fact]
    public void DirectSow_HidesSeedlingAgeWithoutDestroyingItsDraftText() => runtime.Invoke(() =>
    {
        var vm = Model(); vm.Draft.Profile.AgeMinText = "50"; vm.Draft.Profile.AgeMaxText = "60";
        vm.EditorStep = 3;
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        var expander = WpfRuntime.Descendants<Expander>(view).Single(x => x.Header?.ToString()?.StartsWith("Параметры партии") == true);
        expander.IsExpanded = true; WpfRuntime.Layout(view, 1280, 720);
        var age = WpfRuntime.Descendants<TextBox>(view).Single(x => BindingOperations.GetBinding(x, TextBox.TextProperty)?.Path.Path == "AgeMinText");
        Assert.Equal(Visibility.Visible, age.Visibility);
        vm.Draft.DirectSow = true; WpfRuntime.Layout(view, 1280, 720);
        Assert.False(vm.Draft.Profile.ShowSeedlingFields);
        Assert.Equal("50", vm.Draft.Profile.AgeMinText);
    });

    [Theory]
    [InlineData(1920, 900, 96)]
    [InlineData(1280, 720, 144)]
    public void PlanningView_LayoutsAndBindings_WorkAtBothScales(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = Model();
        vm.CalculateAsync().GetAwaiter().GetResult();
        vm.EditorStep = 4;
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, width, height);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding)
                Assert.Equal(BindingStatus.Active, binding.Status);
        Assert.Contains("Возможный посев", Assert.IsType<TextBlock>(view.FindName("PlanningPreviewText")).Text);
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"planning-{width}-{dpi}.png");
        Render(view, width, height, dpi, path);
        output.WriteLine(path);
    });

    [Fact]
    public void LibrariesView_LoadsAndRendersWithoutBindingErrors() => runtime.Invoke(() =>
    {
        var service = Service();
        var vm = new PlanningLibrariesViewModel(service);
        vm.LoadAsync().GetAwaiter().GetResult();
        var view = new PlanningLibrariesView { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding)
                Assert.Equal(BindingStatus.Active, binding.Status);
    });

    [Theory]
    [InlineData(1920, 900, 96)]
    [InlineData(1280, 720, 144)]
    public void HarvestEditor_ExpandedBindingsAndInvalidInput(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = Model();
        vm.Draft.Harvest.ApplyBed(1, "Грядка", 2m, .9m);
        vm.EditorStep = 2;
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, width, height);
        var expander = WpfRuntime.Descendants<Expander>(view).Single(x => x.Header?.ToString()?.StartsWith("Грядка,") == true);
        expander.IsExpanded = true;
        WpfRuntime.Layout(view, width, height);
        var editor = WpfRuntime.Descendants<HarvestPlanningEditor>(view).Single();
        foreach (var box in WpfRuntime.Descendants<TextBox>(editor))
            Assert.Equal(BindingStatus.Active, BindingOperations.GetBindingExpression(box, TextBox.TextProperty)!.Status);
        var length = WpfRuntime.Descendants<TextBox>(editor).Single(x => BindingOperations.GetBinding(x, TextBox.TextProperty)?.Path.Path == "Length");
        length.Text = "ошибка"; WpfRuntime.Layout(view, width, height);
        Assert.True(Validation.GetHasError(length)); Assert.False(vm.CalculateCommand.CanExecute(null));
        Assert.Equal("ошибка", vm.Draft.Harvest.Length);
        length.Text = "2";
        WpfRuntime.Layout(view, width, height);
        Assert.True(vm.Draft.Harvest.Build().IsSuccess);
        WpfRuntime.Descendants<ScrollViewer>(view).First(x => x.Content is StackPanel && x.ScrollableHeight > 0).ScrollToVerticalOffset(450);
        WpfRuntime.Layout(view, width, height);
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"harvest-{width}-{dpi}.png"); Render(view, width, height, dpi, path); output.WriteLine(path);
    });

    [Theory]
    [InlineData(1920, 900, 96)]
    [InlineData(1280, 720, 144)]
    public void HarvestJournal_RealBindingsValidationAndLayout(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = HarvestJournalWorkflowTests.Model(Substitute.For<IHarvestService>());
        var view = new HarvestJournalView { DataContext = vm }; WpfRuntime.Layout(view, width, height);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding) Assert.Equal(BindingStatus.Active, binding.Status);
        var mass = Assert.IsType<TextBox>(view.FindName("HarvestMassInput")); mass.Text = "не число"; WpfRuntime.Layout(view, width, height);
        Assert.True(Validation.GetHasError(mass)); Assert.False(vm.SaveCommand.CanExecute(null)); Assert.Equal("не число", vm.Draft.MassKg);
        mass.Text = "1,25"; WpfRuntime.Layout(view, width, height); Assert.True(vm.SaveCommand.CanExecute(null));
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"journal-{width}-{dpi}.png"); Render(view, width, height, dpi, path); output.WriteLine(path);
    });

    [Theory]
    [InlineData(1920, 900, 96)]
    [InlineData(1280, 720, 144)]
    public void PerennialView_LifeCycleAndSeasonBindingsAtBothScales(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = PerennialPlanningWorkflowTests.Model(Substitute.For<IPerennialPlanningService>());
        var view = new PerennialPlanningView { DataContext = vm }; WpfRuntime.Layout(view, width, height);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding) Assert.Equal(BindingStatus.Active, binding.Status);
        var year = WpfRuntime.Descendants<TextBox>(view).Single(x => BindingOperations.GetBinding(x, TextBox.TextProperty)?.Path.Path == "Draft.EstablishmentYear");
        year.Text = "ошибка"; WpfRuntime.Layout(view, width, height); Assert.True(Validation.GetHasError(year)); Assert.False(vm.SaveCommand.CanExecute(null));
        Assert.Equal("ошибка", vm.Draft.EstablishmentYear); year.Text = "2026"; WpfRuntime.Layout(view, width, height);
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var body = Path.Combine(folder, $"perennial-body-{width}-{dpi}.png"); Render(view, width, height, dpi, body); output.WriteLine(body);
        WpfRuntime.Descendants<TabControl>(view).Single().SelectedIndex = 1; WpfRuntime.Layout(view, width, height);
        vm.CalculateAsync().GetAwaiter().GetResult(); WpfRuntime.Layout(view, width, height);
        Assert.NotNull(vm.Preview); Assert.Contains("2027", vm.PreviewText);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding) Assert.Equal(BindingStatus.Active, binding.Status);
        var season = Path.Combine(folder, $"perennial-season-{width}-{dpi}.png"); Render(view, width, height, dpi, season); output.WriteLine(season);
    });

    private static PlantingPlanningViewModel Model()
    {
        return BatchModel();
    }

    [Fact]
    public void Libraries_BothSectionsAndProfileRenderForVisualReview() => runtime.Invoke(() =>
    {
        var service = Service(); var libraries = new PlanningLibrariesViewModel(service); libraries.LoadAsync().GetAwaiter().GetResult();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        var view = new Bonfire.Views.LibraryEditorWindow { DataContext = new LibraryEditorViewModel(Substitute.For<ILibraryService>(), Substitute.For<IUserDialog>(), seeds, libraries) };
        WpfRuntime.Layout(view, 1280, 720);
        var root = WpfRuntime.Descendants<TabControl>(view).First();
        var folder = Path.Combine(Path.GetTempPath(), "BonfirePlanningUi", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var original = Path.Combine(folder, "libraries-original-1280-144.png"); Render(view, 1280, 720, 144, original); output.WriteLine(original);
        root.SelectedIndex = 1; WpfRuntime.Layout(view, 1280, 720);
        var planning = Path.Combine(folder, "libraries-planning-1280-144.png"); Render(view, 1280, 720, 144, planning); output.WriteLine(planning);
        var inner = WpfRuntime.Descendants<TabControl>(view).Single(x => !ReferenceEquals(x, root)); inner.SelectedIndex = 1;
        libraries.CultureFields.LifeCycle.Mode = 2; libraries.CultureFields.LifeCycle.FirstHarvest = "2";
        WpfRuntime.Layout(view, 1280, 720);
        foreach (var expander in WpfRuntime.Descendants<Expander>(view).Where(x => x.Header?.ToString() == "Жизненный цикл, цветение и плодоношение")) expander.IsExpanded = true;
        WpfRuntime.Layout(view, 1280, 720);
        foreach (var box in WpfRuntime.Descendants<TextBox>(view))
            if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding) Assert.Equal(BindingStatus.Active, binding.Status);
        var profiles = Path.Combine(folder, "libraries-profiles-1280-144.png"); Render(view, 1280, 720, 144, profiles); output.WriteLine(profiles);
    });

    private static PlantingPlanningViewModel BatchModel()
    {
        var service = Service();
        var vm = new PlantingPlanningViewModel(service, Substitute.For<IUserDialog>(), new(service), TimeProvider.System, Substitute.For<IReportService>());
        var summary = new PlantingPlanSummaryFromViewModel(1, 1, "Сезон", 2027, "Томск");
        var choice = new PlanningChoiceFromViewModel(1, "Томат", 2, "Черри");
        vm.Plans.Add(summary); vm.Choices.Add(choice);
        vm.OpenPlanAsync(summary).GetAwaiter().GetResult();
        vm.Draft.Choice = choice;
        vm.Draft.TransplantStartText = "20.05.2027"; vm.Draft.TransplantEndText = "25.05.2027"; vm.Draft.ManualSeedCountText = "30";
        return vm;
    }

    private static IPlanningService Service()
    {
        var service = Substitute.For<IPlanningService>();
        service.ReadPlanAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success(Option.Some(PlanningWorkflowTests.Plan())));
        service.ReadRegionsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success(System.Collections.Immutable.ImmutableArray<RegionFromViewModel>.Empty));
        service.ReadChoicesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(System.Collections.Immutable.ImmutableArray<PlanningChoiceFromViewModel>.Empty));
        service.ReadContextAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success(PlanningWorkflowTests.ReferenceContext()));
        service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>()).Returns(call => Result.Success(PlanningWorkflowTests.Preview(call.Arg<PlanningBatchInputFromViewModel>())));
        return service;
    }

    private static void Render(FrameworkElement view, int width, int height, int dpi, string path)
    {
        if (VisualTreeHelper.GetParent(view) is null)
        {
            var host = new Border { Width = width, Height = height, Child = view };
            WpfRuntime.Layout(host, width, height);
        }
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            var brush = new VisualBrush(view) { AutoLayoutContent = false, ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, width, height),
                Stretch = Stretch.Fill, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
            drawing.DrawRectangle(brush, null, new Rect(0, 0, width, height));
        }
        var bitmap = new RenderTargetBitmap(width * dpi / 96, height * dpi / 96, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
