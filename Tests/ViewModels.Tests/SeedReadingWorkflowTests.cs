using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;

namespace ViewModels.Tests;

public class SeedReadingWorkflowTests
{
    [Fact]
    public void Reload_OptionsRefused_PreservesOptionsSelectionAndDraft()
    {
        var original = SeedsWorkflowTests.Seed(1).Read();
        var service = Substitute.For<ISeedsService>(); var dialog = Substitute.For<IUserDialog>();
        var options = new SeedEditorOptionsFromViewModel(["Красный"], [], []);
        service.GetEditorOptionsAsync().Returns(Result.Success(options));
        var vm = new SeedsViewModel(service, dialog, Substitute.For<IReportService>()) { Seeds = [original] };
        vm.LoadDataCommand.Execute(null); vm.SelectedItem = original; var draft = vm.EditedItem!;
        draft.Package.Note = "Правка";
        service.GetEditorOptionsAsync().Returns(Result.Failure<SeedEditorOptionsFromViewModel>("Ошибка чтения вариантов"));
        vm.LoadDataCommand.Execute(null);
        Assert.Same(options, vm.EditorOptions); Assert.Same(original, vm.SelectedItem); Assert.Same(draft, vm.EditedItem);
        Assert.Equal("Правка", draft.Package.Note); dialog.Received(1).Error("Ошибка чтения вариантов", Arg.Any<string>());
    }

    [Fact]
    public void LibrarySave_ImmutableCache_UpdatesSelectionButPreservesDraftAndOriginalSnapshot()
    {
        var original = SeedsWorkflowTests.Seed(1).Read();
        var dialog = Substitute.For<IUserDialog>();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), dialog, Substitute.For<IReportService>()) { Seeds = [original] };
        seeds.SelectedItem = original; var draft = seeds.EditedItem!; draft.Package.Note = "Мой черновик";
        var library = Substitute.For<ILibraryService>();
        library.GetProducerAsync(1).Returns(Result.Success(new ProducerDetailsFromViewModel(1, "Старое")));
        library.UpdateProducerAsync(1, "Новое").Returns(Result.Success(new ProducerDetailsFromViewModel(1, "Новое")));
        var vm = new LibraryEditorViewModel(library, dialog, seeds) { SelectedProducer = new() { Id = 1 }, ProducerDetailName = "Новое" };
        vm.SaveProducerCommand.Execute(null);
        Assert.Equal("Новое", seeds.SelectedItem!.Plant.PlantSort.Producer.Name);
        Assert.Same(seeds.Seeds[0], seeds.SelectedItem); Assert.Same(draft, seeds.EditedItem);
        Assert.Equal("Мой черновик", draft.Package.Note); Assert.True(seeds.HasUnsavedSeedChanges);
        Assert.Equal("Фирма 1", original.Plant.PlantSort.Producer.Name);
        dialog.DidNotReceive().YesNoQuestion(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Save_RefreshRejected_DoesNotDiscardDirtyDraft()
    {
        var original = SeedsWorkflowTests.Seed(1).Read();
        var dialog = Substitute.For<IUserDialog>(); var service = Substitute.For<ISeedsService>();
        var vm = new SeedsViewModel(service, dialog, Substitute.For<IReportService>()) { Seeds = [original] };
        vm.UpdateCollectionViewSource(); vm.SelectedItem = original; var draft = vm.EditedItem!; draft.Package.Note = "Сохранённый ввод";
        dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        service.SaveSeedEditAsync(Arg.Any<EditSeedFromViewModel>()).Returns(Result.Success(new SeedEditResultFromViewModel(1, 1)));
        service.ReadSeedsAsync().Returns(Result.Failure<ImmutableArray<SeedReadFromViewModel>>("Другая запись повреждена"));
        vm.UpdateSeedsInfoCommand.Execute(null);
        Assert.Same(draft, vm.EditedItem); Assert.Equal("Сохранённый ввод", vm.EditedItem!.Package.Note);
        Assert.Same(original, vm.Seeds[0]); Assert.True(vm.HasUnsavedSeedChanges);
        dialog.Received(1).Error("Другая запись повреждена", Arg.Any<string>());
    }
}
