using System.ComponentModel;
using System.Text.RegularExpressions;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public class SortEditModel : ViewModel, IDataErrorInfo
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    private bool _dirty;

    public int Id { get; init; }

    public string? Name
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public string? Description
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? MinGerminationTime
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? MaxGerminationTime
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? AgeOfSeedlings
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? GrowingSeason
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? LandingPattern
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? PlantHeight
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public string? PlantColor
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public bool IsDirty => _dirty;

    public bool HasErrors =>
        string.IsNullOrWhiteSpace(Name)
        || MinGerminationTime < 0
        || MaxGerminationTime < 0
        || (MinGerminationTime.HasValue && MaxGerminationTime.HasValue && MinGerminationTime > MaxGerminationTime)
        || (!string.IsNullOrEmpty(PlantColor) && !HexColorRegex.IsMatch(PlantColor));

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(Name) when string.IsNullOrWhiteSpace(Name)
            => "Название не может быть пустым",
        nameof(MinGerminationTime) when MinGerminationTime < 0
            => "Значение должно быть ≥ 0",
        nameof(MaxGerminationTime) when MaxGerminationTime < 0
            => "Значение должно быть ≥ 0",
        nameof(MinGerminationTime) when MinGerminationTime.HasValue && MaxGerminationTime.HasValue && MinGerminationTime > MaxGerminationTime
            => "Минимум не может быть больше максимума",
        nameof(PlantColor) when !string.IsNullOrEmpty(PlantColor) && !HexColorRegex.IsMatch(PlantColor)
            => "Формат: #RRGGBB",
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
