using Bonfire.Models;

namespace Bonfire.ViewModels.Base;

/// <summary>
/// Базовый VM с выбором источника получения (радиокнопки) — общий для семян и рассады.
/// Содержит общие флаги «Куплено»/«Подарено»; третий вариант специфичен для наследника
/// (Собрано — у семян, Из семян — у рассады) и уведомляется через <see cref="OnSourceChanged"/>.
/// </summary>
public abstract class SourceSelectionViewModel : ViewModel
{
    private string _source = string.Empty;

    protected string SourceValue
    {
        get => _source;
        set
        {
            if (_source == value) return;
            _source = value;
            OnPropertyChanged(nameof(IsSold));
            OnPropertyChanged(nameof(IsDonated));
            OnSourceChanged();
        }
    }

    /// <summary>Уведомить о специфичном для наследника флаге-источнике.</summary>
    protected virtual void OnSourceChanged() { }

    public bool IsSold
    {
        get => _source == PlantSources.Purchased;
        set { if (value) SourceValue = PlantSources.Purchased; }
    }

    public bool IsDonated
    {
        get => _source == PlantSources.Donated;
        set { if (value) SourceValue = PlantSources.Donated; }
    }
}
