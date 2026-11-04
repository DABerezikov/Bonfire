using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Application.Planning;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel
{
    private ImmutableArray<PlanningChoiceFromViewModel> _allChoices = [];
    private bool _choicesLoaded;
    private long _choicesRevision;
    public bool ShowChoicesWithoutSeeds
    {
        get;
        set { if (Set(ref field, value)) RebuildChoiceList(); }
    }
    public string ChoiceAvailabilityText => Draft.Choice is not { } choice
        ? ShowChoicesWithoutSeeds ? "Показаны также сорта без семян в наличии." : "Показаны сорта с положительным остатком семян."
        : !_choicesLoaded ? "Наличие семян ещё не проверено. Нажмите «Обновить»."
        : _allChoices.Any(item => SameChoice(item, choice) && item.HasAvailableSeeds)
            ? "Семена есть в наличии."
            : "Семян этого сорта нет в наличии. Для посадки их нужно приобрести; партия остаётся в плане.";

    private async Task<bool> RefreshChoicesAsync()
    {
        var revision = ++_choicesRevision;
        var result = await _service.ReadChoicesAsync();
        if (revision != _choicesRevision) return false;
        return result.Match(items =>
        {
            _allChoices = items; _choicesLoaded = true; RebuildChoiceList(); return true;
        }, error => { Error(error); return false; });
    }

    private void RebuildChoiceList()
    {
        var choice = Draft.Choice;
        var desired = _allChoices.Where(item => ShowChoicesWithoutSeeds || item.HasAvailableSeeds)
            .Select(item => choice is not null && SameChoice(item, choice) ? choice : item).ToList();
        if (choice is not null && !desired.Any(item => SameChoice(item, choice))) desired.Add(choice);
        // Не удаляем выбранный экземпляр: WPF иначе сбросит SelectedItem и изменит черновик.
        for (var index = Choices.Count - 1; index >= 0; index--)
            if (!desired.Contains(Choices[index])) Choices.RemoveAt(index);
        for (var index = 0; index < desired.Count; index++)
        {
            var current = Choices.IndexOf(desired[index]);
            if (current < 0) Choices.Insert(index, desired[index]);
            else if (current != index) Choices.Move(current, index);
        }
        OnPropertyChanged(nameof(ChoiceAvailabilityText));
    }

    private static bool SameChoice(PlanningChoiceFromViewModel left, PlanningChoiceFromViewModel right)
        => left.CultureId == right.CultureId && left.SortId == right.SortId;
}
