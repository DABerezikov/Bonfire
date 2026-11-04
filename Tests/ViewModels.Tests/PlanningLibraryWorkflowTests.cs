using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Infrastructure.Commands.Base;

namespace ViewModels.Tests;

public class PlanningLibraryWorkflowTests
{
    [Fact]
    public async Task SaveRegion_LateInputAndRepeatedClick_PreserveDraftAndWriteOnce()
    {
        var service = Substitute.For<IPlanningService>();
        var pending = new TaskCompletionSource<Result<RegionFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SaveRegionAsync(Arg.Any<SaveRegionFromViewModel>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var vm = new PlanningLibrariesViewModel(service);
        vm.RegionEditor.Name = "Томск"; vm.RegionEditor.TimeZoneId = "Asia/Tomsk";
        var editor = vm.RegionEditor;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = Assert.IsAssignableFrom<CommandAsync>(vm.SaveRegionCommand);
        command.ExecutableChanged += (_, _) => { if (command.Executable) done.TrySetResult(); };
        vm.SaveRegionCommand.Execute(null);
        vm.SaveRegionCommand.Execute(null);
        editor.Name = "Поздний ввод";
        pending.SetResult(Result.Success(new RegionFromViewModel(RegionId.Create(3).ValueOrThrow(),
            RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow())));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Same(editor, vm.RegionEditor);
        Assert.Equal("Поздний ввод", editor.Name);
        Assert.Equal("Томск", editor.Original.Name);
        Assert.True(editor.IsDirty);
        await service.Received(1).SaveRegionAsync(Arg.Any<SaveRegionFromViewModel>(), Arg.Any<CancellationToken>());
    }
}
