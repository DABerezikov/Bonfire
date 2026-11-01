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
using Bonfire.Views.Planning;
using Xunit.Abstractions;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class PlanningEditorWpfTests(WpfRuntime runtime, ITestOutputHelper output)
{
    [Fact]
    public void InvalidText_RemainsInEditorAndBlocksCalculation() => runtime.Invoke(() =>
    {
        var vm = Model();
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
        var view = new PlantingPlanningView { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        var expander = WpfRuntime.Descendants<Expander>(view).Single(x => x.Header?.ToString()?.StartsWith("Параметры партии") == true);
        expander.IsExpanded = true; WpfRuntime.Layout(view, 1280, 720);
        var age = WpfRuntime.Descendants<TextBox>(view).Single(x => BindingOperations.GetBinding(x, TextBox.TextProperty)?.Path.Path == "AgeMinText");
        Assert.Equal(Visibility.Visible, age.Visibility);
        vm.Draft.DirectSow = true; WpfRuntime.Layout(view, 1280, 720);
        Assert.Equal(Visibility.Collapsed, age.Visibility);
        Assert.Equal("50", vm.Draft.Profile.AgeMinText);
    });

    [Theory]
    [InlineData(1920, 900, 96)]
    [InlineData(1280, 720, 144)]
    public void PlanningView_LayoutsAndBindings_WorkAtBothScales(int width, int height, int dpi) => runtime.Invoke(() =>
    {
        var vm = Model();
        vm.CalculateAsync().GetAwaiter().GetResult();
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

    private static PlantingPlanningViewModel Model()
    {
        var service = Service();
        var vm = new PlantingPlanningViewModel(service, Substitute.For<IUserDialog>(), new(service), TimeProvider.System);
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
        service.ReadContextAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<PlanningContextFromViewModel>("Справочные интервалы не заполнены"));
        service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>()).Returns(call => Result.Success(PlanningWorkflowTests.Preview(call.Arg<PlanningBatchInputFromViewModel>())));
        return service;
    }

    private static void Render(FrameworkElement view, int width, int height, int dpi, string path)
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            drawing.DrawRectangle(new VisualBrush(view), null, new Rect(0, 0, width, height));
        }
        var bitmap = new RenderTargetBitmap(width * dpi / 96, height * dpi / 96, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
