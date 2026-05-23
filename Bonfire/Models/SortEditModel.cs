using System.ComponentModel;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public class SortEditModel : ViewModel, IDataErrorInfo
{
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
        set
        {
            if (Set(ref field, value))
            {
                SetDirty();
                OnPropertyChanged(nameof(MaxGerminationTime));
            }
        }
    }

    public int? MaxGerminationTime
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                SetDirty();
                OnPropertyChanged(nameof(MinGerminationTime));
            }
        }
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
        || AgeOfSeedlings < 0
        || GrowingSeason < 0
        || LandingPattern < 0
        || PlantHeight < 0
        || (MinGerminationTime.HasValue && MaxGerminationTime.HasValue && MinGerminationTime > MaxGerminationTime);

    public string Error => string.Empty;

    private static readonly string NegativeError = "Значение должно быть ≥ 0";

    public string this[string columnName] => columnName switch
    {
        nameof(Name) when string.IsNullOrWhiteSpace(Name)
            => "Название не может быть пустым",
        nameof(MinGerminationTime) when MinGerminationTime < 0
            => NegativeError,
        nameof(MaxGerminationTime) when MaxGerminationTime < 0
            => NegativeError,
        nameof(AgeOfSeedlings) when AgeOfSeedlings < 0
            => NegativeError,
        nameof(GrowingSeason) when GrowingSeason < 0
            => NegativeError,
        nameof(LandingPattern) when LandingPattern < 0
            => NegativeError,
        nameof(PlantHeight) when PlantHeight < 0
            => NegativeError,
        nameof(MinGerminationTime) when MinGerminationTime.HasValue && MaxGerminationTime.HasValue && MinGerminationTime > MaxGerminationTime
            => "Минимум не может быть больше максимума",
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
