using System;
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

    public int? MaxGrowingSeason
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

    public string? Maturity
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? MaxAgeOfSeedlings
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? MaxPlantHeight
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public int? LandingRowSpacing
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public double? YieldKg
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public double? MinFruitWeight
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    public double? MaxFruitWeight
    {
        get;
        set { if (Set(ref field, value)) SetDirty(); }
    }

    // Культура меняется только в редакторе библиотек.
    public int? CultureId
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
        || HasRangeErrors
        || HasIncompletePlantingScheme
        || (MinGerminationTime.HasValue && MaxGerminationTime.HasValue && MinGerminationTime > MaxGerminationTime);

    private bool HasRangeErrors =>
        InvalidRange(AgeOfSeedlings, MaxAgeOfSeedlings)
        || InvalidRange(PlantHeight, MaxPlantHeight)
        || InvalidRange(MinFruitWeight, MaxFruitWeight)
        || InvalidRange(GrowingSeason, MaxGrowingSeason)
        || InvalidRange(YieldKg, null) || HasYieldPrecisionError || LandingRowSpacing < 0;

    private bool HasYieldPrecisionError => YieldKg.HasValue && double.IsFinite(YieldKg.Value)
        && Math.Round(YieldKg.Value, 1) != YieldKg.Value;

    private bool HasIncompletePlantingScheme => LandingPattern.HasValue != LandingRowSpacing.HasValue;

    private static bool InvalidRange(double? min, double? max) =>
        (min.HasValue && (!double.IsFinite(min.Value) || min < 0))
        || (max.HasValue && (!double.IsFinite(max.Value) || max < 0))
        || (min.HasValue && max.HasValue && min > max);

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
        nameof(AgeOfSeedlings) or nameof(MaxAgeOfSeedlings) when InvalidRange(AgeOfSeedlings, MaxAgeOfSeedlings)
            => "Некорректный диапазон возраста рассады",
        nameof(PlantHeight) or nameof(MaxPlantHeight) when InvalidRange(PlantHeight, MaxPlantHeight)
            => "Некорректный диапазон высоты",
        nameof(MinFruitWeight) or nameof(MaxFruitWeight) when InvalidRange(MinFruitWeight, MaxFruitWeight)
            => "Некорректный диапазон массы плода",
        nameof(GrowingSeason) or nameof(MaxGrowingSeason) when InvalidRange(GrowingSeason, MaxGrowingSeason)
            => "Некорректный диапазон вегетации",
        nameof(YieldKg) when InvalidRange(YieldKg, null) => NegativeError,
        nameof(YieldKg) when HasYieldPrecisionError => "Допустим один знак после запятой",
        nameof(LandingRowSpacing) when LandingRowSpacing < 0 => NegativeError,
        nameof(LandingPattern) or nameof(LandingRowSpacing) when HasIncompletePlantingScheme
            => "Заполните оба значения схемы посадки или очистите оба поля",
        _ => string.Empty
    };

    public void ResetDirty()
    {
        _dirty = false;
        OnPropertyChanged(nameof(IsDirty));
    }

    private void SetDirty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        _dirty = true;
        var pairedProperty = propertyName switch
        {
            nameof(GrowingSeason) => nameof(MaxGrowingSeason),
            nameof(MaxGrowingSeason) => nameof(GrowingSeason),
            nameof(AgeOfSeedlings) => nameof(MaxAgeOfSeedlings),
            nameof(MaxAgeOfSeedlings) => nameof(AgeOfSeedlings),
            nameof(PlantHeight) => nameof(MaxPlantHeight),
            nameof(MaxPlantHeight) => nameof(PlantHeight),
            nameof(MinFruitWeight) => nameof(MaxFruitWeight),
            nameof(MaxFruitWeight) => nameof(MinFruitWeight),
            nameof(LandingPattern) => nameof(LandingRowSpacing),
            nameof(LandingRowSpacing) => nameof(LandingPattern),
            _ => null
        };
        if (pairedProperty != null) OnPropertyChanged(pairedProperty);
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(HasErrors));
    }
}
