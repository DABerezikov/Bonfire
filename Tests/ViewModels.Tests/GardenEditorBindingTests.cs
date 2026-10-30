using System.Windows.Controls;
using System.Windows.Data;
using Bonfire.Infrastructure.Converters;
using Bonfire.Views.GardenPlan;
using Microsoft.Xaml.Behaviors;

namespace ViewModels.Tests;

public partial class CanvasInteractionTests
{
    [Fact]
    public void ElementEditor_InvalidNumber_BlocksSavingAndPreservesValue() => runtime.Invoke(() =>
    {
        var vm = new GardenPlanViewModel(Substitute.For<IGardenService>(), Substitute.For<IUserDialog>(),
            Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>());
        var model = new GardenElementFromViewModel { Id = 1, Name = "Грядка", X = 30 };
        vm.SelectedElement = model;
        var view = new GardenPlanView { DataContext = vm };
        Interaction.GetTriggers(view).Clear();
        WpfRuntime.Layout(view);
        var coordinate = WpfRuntime.Descendants<TextBox>(view).First(box => BindingOperations.GetBinding(box, TextBox.TextProperty)?.Converter is PxToMetersConverter);

        coordinate.Text = "не число";
        coordinate.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();

        Assert.True(Validation.GetHasError(coordinate));
        Assert.True(vm.ElementEditorHasBindingErrors);
        Assert.Equal(30, vm.ElementEditor!.X);
        Assert.False(vm.SaveElementPropertiesCommand.CanExecute(null));

        vm.CancelElementPropertiesCommand.Execute(null);
        WpfRuntime.Layout(view);
        Assert.False(Validation.GetHasError(coordinate));
        Assert.False(vm.ElementEditorHasBindingErrors);
        Assert.Equal(30, vm.ElementEditor!.X);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElementEditor_NameInput_DoesNotChangeCanvasModel(bool innerCanvas) => runtime.Invoke(() =>
    {
        var vm = new GardenPlanViewModel(Substitute.For<IGardenService>(), Substitute.For<IUserDialog>(),
            Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>());
        var model = new GardenElementFromViewModel { Id = 1, Name = "Редактируемое", X = 30 };
        vm.SelectedElement = model;
        var view = new GardenPlanView { DataContext = vm };
        Interaction.GetTriggers(view).Clear();
        WpfRuntime.Layout(view);
        var name = WpfRuntime.Descendants<TextBox>(view).Where(box => box.Text == "Редактируемое").ElementAt(innerCanvas ? 1 : 0);

        name.Text = "Черновик";
        name.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();

        Assert.Equal("Редактируемое", model.Name);
    });
}
