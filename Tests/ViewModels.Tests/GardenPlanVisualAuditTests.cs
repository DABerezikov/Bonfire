using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using BonfireDB.Planting;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bonfire.Models.Mappers;
using Bonfire.Services;
using Bonfire.Views.GardenPlan;
using BonfireDB;
using BonfireDB.Context;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;

namespace ViewModels.Tests;

public partial class CanvasInteractionTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(144)]
    public async Task Planner_SavedSqlitePlan_RendersCanvasEditorsAndGrid(int dpi)
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<DbBonfire>(options => options.UseSqlite(connection));
        services.AddRepositoriesInDb(); services.AddServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DbBonfire>(); await db.Database.EnsureCreatedAsync();
            db.Gardens.Add(new Garden { Name = "Сохранённый участок", WidthMeters = 12, HeightMeters = 8, CanvasWidth = 1800, CanvasHeight = 1200,
                GardenPlan = new() { Name = "План 2026", Year = 2026 },
                Elements = [new Bed { Name = "Томаты", X = 15, Y = 15, StateTypeName = nameof(ActiveState),
                    PlantingSpots = [new() { Row = 0, Column = 0, Note = "Черри", StateTypeName = nameof(PlantedSpotState), SeedlingInfo = new() { Note = "История" } }] },
                    new FlowerBed { Name = "Цветник", X = 450, Y = 30, DisplayWidth = 90, DisplayHeight = 90 }],
                Greenhouses = [new() { Name = "Теплица", X = 750, Y = 15, DisplayWidth = 900, DisplayHeight = 450,
                    WidthMeters = 6, HeightMeters = 3, CanvasWidth = 900, CanvasHeight = 450,
                    Elements = [new ColdFrame { Name = "Перцы", X = 15, Y = 15, DisplayWidth = 180, DisplayHeight = 120 }] }] });
            await db.SaveChangesAsync();
        }
        var service = provider.GetRequiredService<IGardenService>();
        var plan = Assert.Single((await service.ReadPlansAsync()).ValueOrThrow());
        var saved = Assert.Single((await service.ReadGardensAsync(plan.Id)).ValueOrThrow());
        var model = GardenPlanMapper.MapGarden(saved);
        Assert.Equal(2, model.Elements.Count); Assert.Single(model.Greenhouses);
        var bed = model.Elements.Single(e => e.Name == "Томаты"); Assert.Equal("Черри", Assert.Single(bed.PlantingSpots).PlantLabel);
        runtime.Invoke(() =>
        {
            var reads = Substitute.For<IGardenService>(); reads.ReadGardensAsync(plan.Id).Returns(Result.Success(ImmutableArray.Create(saved)));
            var vm = new GardenPlanViewModel(reads, Substitute.For<IUserDialog>(), Substitute.For<ISeedlingsService>(),
                Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>());
            var planModel = GardenPlanMapper.MapPlan(plan);
            vm.Plans = [planModel]; vm.SelectedPlan = planModel; vm.Gardens = [model]; vm.SelectedGarden = model;
            var view = new GardenPlanView { DataContext = vm, Background = Brushes.White };
            Interaction.GetTriggers(view).Clear();
            WpfRuntime.Layout(view, 1600, 900);
            Assert.Equal(2, WpfRuntime.Descendants<GardenElementControl>(view).Count());
            Assert.Single(WpfRuntime.Descendants<GreenhouseControl>(view));
            SaveVisual(view, "garden", dpi);
            vm.OpenGreenhouseCommand.Execute(model.Greenhouses[0]); WpfRuntime.Layout(view, 1600, 900);
            Assert.True(vm.IsEditingGreenhouse); Assert.NotNull(vm.GreenhouseEditor);
            SaveVisual(view, "greenhouse", dpi);
            vm.CloseGreenhouseCommand.Execute(null); vm.SelectedElement = bed; vm.OpenPlantingGridCommand.Execute(null);
            WpfRuntime.Layout(view, 1600, 900); Assert.True(vm.IsGridOpen);
            SaveVisual(view, "grid", dpi);
        });
    }

    private static void SaveVisual(FrameworkElement view, string name, int dpi)
    {
        var bitmap = new RenderTargetBitmap((int)(view.ActualWidth * dpi / 96), (int)(view.ActualHeight * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var directory = Path.Combine(AppContext.BaseDirectory, "visual", "stage6"); Directory.CreateDirectory(directory);
        using var output = File.Create(Path.Combine(directory, $"{name}-{dpi}.png")); encoder.Save(output);
    }
}
