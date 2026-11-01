using System.Windows.Controls;
using System.Windows.Data;
using Bonfire.Views;
using MoonCalendar;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class SeedlingEditorWpfTests(WpfRuntime runtime)
{
    [Fact]
    public void Editor_InvalidQuantityBlocksSaveAndCorrectionRestoresIt() => runtime.Invoke(() =>
    {
        var batch = new Seedling { Id = 7, Quantity = 3, Plant = SeedsWorkflowTests.Seed(1).Plant,
            SeedlingInfos = [new() { Id = 1, LandingDate = DateTime.Today, SeedlingSource = "Из семян" }] };
        var service = Substitute.For<ISeedlingsService>();
        service.ReadSeedlingsAsync().Returns(new[] { batch }.ReadSeedlings());
        service.Lunar.Returns(new Bonfire.Infrastructure.MoonPhaseCalendar(new Bonfire.Infrastructure.AstronomyLunarPhaseCalculator()));
        var seeds = Substitute.For<ISeedsService>(); seeds.ReadSeedsAsync().Returns(Array.Empty<Seed>().ReadSeeds());
        var vm = new SeedlingsViewModel(service, seeds, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null);
        vm.SelectedSeedlingViewItem = Assert.Single(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
        var view = new SeedlingsWindow { DataContext = vm };
        WpfRuntime.Layout(view);
        var grid = WpfRuntime.Descendants<DataGrid>(view).First();
        grid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;
        WpfRuntime.Layout(view);
        var input = WpfRuntime.Descendants<TextBox>(view).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "DataContext.EditedItem.Quantity");
        var preview = new RenderTargetBitmap(1920, 900, 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen()) drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, 1920, 900));
        preview.Render(background);
        preview.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(preview));
        var folder = Path.Combine(AppContext.BaseDirectory, "artifacts");
        Directory.CreateDirectory(folder);
        using (var output = File.Create(Path.Combine(folder, "seedling-editor.png"))) encoder.Save(output);
        input.Text = "ошибка";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.False(vm.UpdateSeedlingInfoCommand.CanExecute(null));
        input.Text = "5";
        input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Assert.True(vm.UpdateSeedlingInfoCommand.CanExecute(null));
        Assert.Equal(5, vm.EditedItem!.Quantity);
        Assert.Equal(3, batch.Quantity);
    });
}
