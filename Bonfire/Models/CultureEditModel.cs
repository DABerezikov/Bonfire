using System.ComponentModel;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public class CultureEditModel : ViewModel, IDataErrorInfo
{
    private bool _dirty;

    public int Id { get; init; }

    public string? Name
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public string Class
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    } = string.Empty;

    public bool IsDirty => _dirty;

    public bool HasErrors => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Class);

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(Name) when string.IsNullOrWhiteSpace(Name) => "Название не может быть пустым",
        nameof(Class) when string.IsNullOrWhiteSpace(Class) => "Класс должен быть выбран",
        _ => string.Empty
    };

    public void ResetDirty()
    {
        _dirty = false;
        OnPropertyChanged(nameof(IsDirty));
    }

    private void SetDirty()
    {
        _dirty = true;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(HasErrors));
    }
}
