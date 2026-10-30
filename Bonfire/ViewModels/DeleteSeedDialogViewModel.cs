using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class DeleteSeedDialogViewModel(string sortName, string? previousReason = null) : ViewModel
{
    public string Prompt { get; } = $"Удалить запись семян сорта «{sortName}»? Несохранённые изменения будут сброшены.";

    public string Reason
    {
        get;
        set => Set(ref field, value);
    } = previousReason ?? string.Empty;
}
