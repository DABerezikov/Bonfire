using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddElement_InvalidGeometry_ReportsErrorWithoutSaving(bool invalidObstacle)
    {
        var vm = CreateVm();
        vm.SelectedGarden = GardenVm();
        if (invalidObstacle)
            vm.SelectedGarden.Elements.Add(new() { Width = double.NaN, Height = 80 });
        else
            vm.SelectedGarden.CanvasWidth = double.NaN;
        var initialCount = vm.SelectedGarden.Elements.Count;
        _gardens.AddElementAsync(Arg.Any<GardenElement>()).Returns(call => call.Arg<GardenElement>());

        vm.AddElementCommand.Execute(null);

        Assert.Equal(initialCount, vm.SelectedGarden.Elements.Count);
        _gardens.DidNotReceive().AddElementAsync(Arg.Any<GardenElement>());
        _dialog.Received(1).Warning(Arg.Is<string>(message => message.Contains("конеч")), "Добавление элемента");
    }
}
