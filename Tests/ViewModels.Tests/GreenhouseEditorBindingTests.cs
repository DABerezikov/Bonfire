using System.Windows.Controls;
using System.Windows.Data;
using Bonfire.Views.GardenPlan;
using Microsoft.Xaml.Behaviors;

namespace ViewModels.Tests;

public partial class CanvasInteractionTests
{
    [Theory]
    [InlineData("не число")]
    [InlineData("NaN")]
    [InlineData("-1")]
    [InlineData("Infinity")]
    public void GreenhouseEditor_InvalidDimension_PreservesValueAndCancelClearsError(string text) => runtime.Invoke(() =>
    {
        var vm = new GardenPlanViewModel(Substitute.For<IGardenService>(), Substitute.For<IUserDialog>(),
            Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>());
        var model = new GreenhouseFromViewModel { Id = 1, WidthMeters = 6, HeightMeters = 3 };
        vm.SelectedGreenhouse = model;
        var view = new GardenPlanView { DataContext = vm };
        Interaction.GetTriggers(view).Clear();
        WpfRuntime.Layout(view);
        var width = WpfRuntime.Descendants<TextBox>(view).Single(box => ReferenceEquals(box.DataContext, model)
            && BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path.EndsWith("WidthMeters") == true);

        width.Text = text;
        width.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();

        Assert.True(Validation.GetHasError(width));
        Assert.True(vm.GreenhouseEditorHasBindingErrors);
        Assert.Equal(6, vm.GreenhouseEditor!.WidthMeters);
        Assert.False(vm.SaveGreenhouseSizeCommand.CanExecute(null));
        vm.CancelGreenhousePropertiesCommand.Execute(null);
        WpfRuntime.Layout(view);
        Assert.False(Validation.GetHasError(width));
        Assert.False(vm.GreenhouseEditorHasBindingErrors);
    });

    [Fact]
    public void GreenhouseEditor_NameInput_DoesNotChangeCanvasModel() => runtime.Invoke(() =>
    {
        var vm = new GardenPlanViewModel(Substitute.For<IGardenService>(), Substitute.For<IUserDialog>(),
            Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>());
        var model = new GreenhouseFromViewModel { Id = 1, Name = "Редактируемая теплица", WidthMeters = 6, HeightMeters = 3 };
        vm.SelectedGreenhouse = model;
        var view = new GardenPlanView { DataContext = vm };
        Interaction.GetTriggers(view).Clear();
        WpfRuntime.Layout(view);
        var name = WpfRuntime.Descendants<TextBox>(view).Single(box => box.Text == "Редактируемая теплица");

        name.Text = "Черновик";
        name.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();

        Assert.Equal("Редактируемая теплица", model.Name);
    });
}
