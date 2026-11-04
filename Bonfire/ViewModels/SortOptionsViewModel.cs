using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class SortOptionsViewModel(IManageSortOptions options, IUserDialog dialog, SeedsViewModel seeds)
    : ViewModel, IDataErrorInfo
{
    public ImmutableArray<SortOptionFromViewModel> Items { get; private set; } = [];

    public SortOptionCategory Category
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            Selected = null;
            OnPropertyChanged(nameof(FilteredItems));
            OnPropertyChanged(nameof(NameRule));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public SortOptionFromViewModel[] FilteredItems => Items.Where(item => item.Category == Category).ToArray();

    public SortOptionFromViewModel? Selected
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            Name = value?.Name ?? string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public PlanningFieldRule NameRule => PlanningFieldRule.Checked("Название", true, SortOptionName.Create(Category, Name));
    public string Error => NameRule.Error;
    public string this[string fieldName] => fieldName == nameof(Name) ? NameRule.Error : string.Empty;

    public string Name
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            OnPropertyChanged(nameof(NameRule));
            CommandManager.InvalidateRequerySuggested();
        }
    } = string.Empty;

    public string Status { get; private set => Set(ref field, value); } = string.Empty;

    public bool IsBusy
    {
        get;
        private set
        {
            if (!Set(ref field, value)) return;
            OnPropertyChanged(nameof(IsEditable));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private long? _loadedVersion;
    public bool IsEditable => !IsBusy;
    public ICommand EnsureLoadedCommand => field ??= new LambdaCommandAsync(async () =>
    {
        if (_loadedVersion != options.ReadVersion) await ReloadAsync();
    }, () => !IsBusy);
    public ICommand LoadCommand => field ??= new LambdaCommandAsync(async () => { options.RefreshReadCache(); await ReloadAsync(); }, () => !IsBusy);
    public ICommand NewCommand => field ??= new LambdaCommand(() =>
    {
        Selected = null;
        Name = string.Empty;
    }, () => !IsBusy);
    public ICommand SaveCommand => field ??= new LambdaCommandAsync(SaveAsync, () => !IsBusy && NameRule.Validation.IsSuccess
        && (Selected is null || Selected.Name != Name.Trim()));
    public ICommand DeleteCommand => field ??= new LambdaCommandAsync(DeleteAsync, () => !IsBusy && Selected is not null);

    private async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> LoadAsync()
    {
        var version = options.ReadVersion;
        return (await options.ReadAsync()).Match(items =>
        {
            Items = items;
            _loadedVersion = version;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(FilteredItems));
            return true;
        }, Fail);
    }

    private bool Fail(string error)
    {
        Status = error;
        dialog.Error(error);
        return false;
    }

    private async Task SaveAsync()
    {
        var original = Selected;
        var name = Name;
        IsBusy = true;
        try
        {
            var result = await options.SaveAsync(Category,
                original is null ? Option.None<string>() : Option.Some(original.Name), name);
            await result.Match(async saved =>
            {
                if (!await LoadAsync()) return;
                Selected = Items.FirstOrDefault(item => item.Category == saved.Category && item.Name == saved.Name);
                (await seeds.RefreshLibrariesAsync()).Match(_ =>
                {
                    Status = "Сохранено. Название обновлено во всех связанных сортах.";
                    return true;
                }, error =>
                {
                    Status = "Изменения записаны, обновление семян не завершено: " + error;
                    return false;
                });
            }, error =>
            {
                Fail(error);
                return Task.CompletedTask;
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } item) return;
        if (!dialog.YesNoQuestion($"Удалить значение «{item.Name}»?", "Редактор библиотек")) return;
        IsBusy = true;
        try
        {
            await (await options.DeleteAsync(item.Category, item.Name)).Match(async _ =>
            {
                if (!await LoadAsync()) return;
                Selected = null;
                (await seeds.RefreshLibrariesAsync()).Match(_ =>
                {
                    Status = "Значение удалено.";
                    return true;
                }, error =>
                {
                    Status = "Значение удалено, обновление семян не завершено: " + error;
                    return false;
                });
            }, error =>
            {
                Fail(error);
                return Task.CompletedTask;
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
