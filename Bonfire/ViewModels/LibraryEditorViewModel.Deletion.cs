using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Libraries;
using Bonfire.Infrastructure.Commands;

namespace Bonfire.ViewModels;

public partial class LibraryEditorViewModel
{
    private bool _isDeleting;
    public ICommand DeleteSortCommand => field ??= new LambdaCommandAsync(
        () => DeleteEntryAsync(LibraryEntryKind.Sort, SelectedSort!.Id, SelectedSort.Name ?? ""),
        () => !_isDeleting && SelectedSort is not null);
    public ICommand DeleteCultureCommand => field ??= new LambdaCommandAsync(
        () => DeleteEntryAsync(LibraryEntryKind.Culture, SelectedCulture!.Id, SelectedCulture.Name ?? ""),
        () => !_isDeleting && SelectedCulture is not null);
    public ICommand DeleteProducerCommand => field ??= new LambdaCommandAsync(
        () => DeleteEntryAsync(LibraryEntryKind.Producer, SelectedProducer!.Id, SelectedProducer.Name ?? ""),
        () => !_isDeleting && SelectedProducer is not null);

    private async Task DeleteEntryAsync(LibraryEntryKind kind, int id, string name)
    {
        if (!_userDialog.YesNoQuestion($"Удалить «{name}» из библиотеки?", "Редактор библиотек")) return;
        _isDeleting = true;
        try
        {
            await (await _libraryService.DeleteEntryAsync(kind, id)).Match(async _ =>
            {
                if (kind == LibraryEntryKind.Sort && SelectedSort?.Id == id) SelectedSort = null;
                if (kind == LibraryEntryKind.Culture && SelectedCulture?.Id == id) SelectedCulture = null;
                if (kind == LibraryEntryKind.Producer && SelectedProducer?.Id == id) SelectedProducer = null;
                await _seedsViewModel.RefreshLibrariesAsync();
            }, error => { _userDialog.Error(error); return Task.CompletedTask; });
        }
        finally { _isDeleting = false; }
    }
}
