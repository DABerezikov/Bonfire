using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bonfire.Application.Seeds;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Views;
using BonfireDB;
using BonfireDB.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class InventoryReadWpfTests(WpfRuntime runtime)
{
    [Theory]
    [InlineData(false, "values")]
    [InlineData(true, "values")]
    [InlineData(false, "absent")]
    [InlineData(true, "absent")]
    [InlineData(false, "empty")]
    [InlineData(true, "empty")]
    [InlineData(false, "minimum")]
    [InlineData(true, "minimum")]
    [InlineData(false, "maximum")]
    [InlineData(true, "maximum")]
    public async Task InformationPanel_SqliteSnapshots_ShowValuesWithoutBindingErrors(bool seedlings, string state)
    {
        var (seed, batch) = await ReadSnapshots(state);
        runtime.Invoke(() =>
        {
            var source = PresentationTraceSources.DataBindingSource; var level = source.Switch.Level;
            using var errors = new BindingErrors(); source.Listeners.Add(errors); source.Switch.Level = SourceLevels.Error;
            try
            {
                var seeds = Substitute.For<ISeedsService>(); seeds.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray.Create(seed)));
                seeds.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])));
                var dialog = Substitute.For<IUserDialog>(); var reports = Substitute.For<IReportService>();
                FrameworkElement view;
                if (seedlings)
                {
                    var service = Substitute.For<ISeedlingsService>(); service.Lunar.Returns(new MoonPhase());
                    service.ReadSeedlingsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray.Create(batch)));
                    var vm = new SeedlingsViewModel(service, seeds, dialog, reports); vm.LoadDataCommand.Execute(null);
                    vm.SelectedSeedlingViewItem = Assert.Single(vm.SeedlingsView!.Cast<SeedlingFromViewModel>());
                    view = new SeedlingsWindow { DataContext = vm };
                }
                else
                {
                    var vm = new SeedsViewModel(seeds, dialog, reports); vm.LoadDataCommand.Execute(null);
                    vm.SelectedSeedsViewItem = Assert.Single(vm.SeedsView!.Cast<SeedsFromViewModel>());
                    view = new SeedsWindow { DataContext = vm };
                }
                WpfRuntime.Layout(view);
                string Text(string field)
                {
                    var block = Assert.Single(WpfRuntime.Descendants<TextBlock>(view).Where(block =>
                        BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path.Path.Trim().EndsWith("." + field, StringComparison.Ordinal) == true
                        || BindingOperations.GetMultiBinding(block, TextBlock.TextProperty)?.Bindings.OfType<Binding>().FirstOrDefault()?.Path.Path.EndsWith("." + field, StringComparison.Ordinal) == true));
                    Assert.Equal(BindingStatus.Active, BindingOperations.GetBindingExpressionBase(block, TextBlock.TextProperty)!.Status);
                    if (field is "PlantColor" or "PlantHeight" or "Description")
                        Assert.Equal(state != "values" && !(state == "empty" && field == "PlantHeight")
                            ? Visibility.Collapsed : Visibility.Visible, block.Visibility);
                    return block.Text;
                }
                Assert.Equal(state == "values" ? "Красный" : "", Text("PlantColor"));
                Assert.Equal(state == "values" ? "от 150" : state == "empty" ? "от 0" : "", Text("PlantHeight"));
                Assert.Equal(state switch { "values" => "2 - 5", "minimum" => "от 2", "maximum" => "до 5", "empty" => "0 - 0", _ => "" }, Text("MinFruitSize"));
                if (state == "values")
                {
                    var panel = Assert.Single(WpfRuntime.Descendants<SortInformationPanel>(view));
                    var height = Assert.IsType<TextBlock>(panel.FindName("PlantHeightValue"));
                    var size = Assert.IsType<TextBlock>(panel.FindName("MinFruitSizeValue"));
                    Assert.InRange(size.TranslatePoint(new Point(), panel).Y - height.TranslatePoint(new Point(), panel).Y, 1, 23);
                }
                if (state == "absent")
                    Assert.InRange(Assert.Single(WpfRuntime.Descendants<SortInformationPanel>(view)).ActualHeight, 0, 20);
                Assert.Equal(state == "values" ? "Описание сорта" : "", Text("Description"));
                if (!seedlings)
                {
                    Assert.Equal(state == "values" ? "Подарено" : "", Text("SeedSource"));
                    Assert.Equal(state == "values" ? "Примечание пакета" : "", Text("Note"));
                }
                foreach (var block in WpfRuntime.Descendants<TextBlock>(view).Where(block =>
                    BindingOperations.GetBinding(block, TextBlock.TextProperty)?.Path.Path.StartsWith("SelectedItem.", StringComparison.Ordinal) == true))
                    Assert.Equal(BindingStatus.Active, block.GetBindingExpression(TextBlock.TextProperty)!.Status);
                Assert.Empty(errors.Messages);
                if (state == "values") foreach (var dpi in new[] { 96, 144 }) Render(view, seedlings ? "seedlings" : "seeds", dpi);
            }
            finally { source.Listeners.Remove(errors); source.Switch.Level = level; }
        });
    }

    private static async Task<(SeedReadFromViewModel, SeedlingReadFromViewModel)> ReadSnapshots(string state)
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var services = new ServiceCollection(); services.AddDbContext<DbBonfire>(options => options.UseSqlite(connection)); services.AddRepositoriesInDb();
        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DbBonfire>(); await db.Database.EnsureCreatedAsync();
            var seed = SeedsWorkflowTests.Seed(1); seed.Id = 0;
            seed.Plant.PlantSort.PlantColor = state == "values" ? "Красный" : state == "empty" ? "" : null;
            seed.Plant.PlantSort.PlantHeight = state == "values" ? 150 : state == "empty" ? 0 : null;
            seed.Plant.PlantSort.MinFruitSize = state is "values" or "minimum" ? 2 : state == "empty" ? 0 : null;
            seed.Plant.PlantSort.MaxFruitSize = state is "values" or "maximum" ? 5 : state == "empty" ? 0 : null;
            seed.Plant.PlantSort.Description = state == "values" ? "Описание сорта" : state == "empty" ? "" : null;
            seed.SeedsInfo.SeedSource = state == "values" ? "Подарено" : state == "empty" ? "" : null;
            seed.SeedsInfo.Note = state == "values" ? "Примечание пакета" : state == "empty" ? "" : null;
            db.Add(seed); db.Add(new Seedling { Plant = seed.Plant, Quantity = 10,
                SeedlingInfos = [new() { LandingDate = new(2026, 3, 1), SeedlingSource = "Из семян" }] });
            await db.SaveChangesAsync();
        }
        return (Assert.Single((await provider.GetRequiredService<ISeedReader>().ReadAsync(default)).ValueOrThrow()),
            Assert.Single((await provider.GetRequiredService<ISeedlingReader>().ReadAsync(default)).ValueOrThrow()));
    }

    internal static void Render(FrameworkElement view, string name, int dpi)
    {
        var image = new RenderTargetBitmap((int)(view.ActualWidth * dpi / 96), (int)(view.ActualHeight * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen()) drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, view.ActualWidth, view.ActualHeight));
        image.Render(background); image.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        var directory = Path.Combine(AppContext.BaseDirectory, "visual", "stage7"); Directory.CreateDirectory(directory);
        using var output = File.Create(Path.Combine(directory, $"{name}-{dpi}.png")); encoder.Save(output);
    }

    private sealed class BindingErrors : TraceListener
    {
        public List<string> Messages { get; } = [];
        public override void Write(string? message) { if (!string.IsNullOrEmpty(message)) Messages.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }
}
