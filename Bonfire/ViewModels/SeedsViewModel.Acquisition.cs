using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Models;

namespace Bonfire.ViewModels;

public partial class SeedsViewModel
{
    private SeedCatalogFromViewModel? _catalog;
    private bool _updatingAcquisition;
    public string AcquisitionSelectionHint { get; private set; } = string.Empty;

    private static bool SameAcquisitionName(string left, string right) => string.Equals(
        LibraryName.NormalizeWhitespace(left), LibraryName.NormalizeWhitespace(right), StringComparison.OrdinalIgnoreCase);

    private CultureDetailsFromViewModel[] AcquisitionCultures => _catalog?.Cultures
        .Where(culture => SameAcquisitionName(culture.Name, AddCulture)).ToArray() ?? [];
    private CultureDetailsFromViewModel? AcquisitionCulture => AcquisitionCultures
        .FirstOrDefault(culture => culture.Class == AddClass) ?? AcquisitionCultures.FirstOrDefault();
    private CatalogSortFromViewModel[] AcquisitionSorts => _catalog?.Sorts.Where(sort =>
        SameAcquisitionName(sort.Name, AddSort) && SortInScope(sort)).ToArray() ?? [];

    private bool SortInScope(CatalogSortFromViewModel sort)
    {
        if (AcquisitionCulture is { } culture)
            return AcquisitionCultures.Any(candidate => candidate.Class == culture.Class && sort.CultureIds.Contains(candidate.Id));
        if (!string.IsNullOrWhiteSpace(AddCulture)) return false;
        return string.IsNullOrEmpty(AddClass) || _catalog!.Cultures.Any(culture =>
            culture.Class == AddClass && sort.CultureIds.Contains(culture.Id));
    }

    private bool MatchesAcquisitionCulture(object item) => _catalog is null || item is CultureFromViewModel culture &&
        _catalog.Cultures.Any(candidate => candidate.Id == culture.Id &&
            (string.IsNullOrEmpty(AddClass) || candidate.Class == AddClass));
    private bool MatchesAcquisitionSort(object item) => _catalog is null || item is SortFromSeedsViewModel sort &&
        _catalog.Sorts.Any(candidate => candidate.Id == sort.Id && SortInScope(candidate));
    private bool MatchesAcquisitionProducer(object item)
    {
        if (_catalog is null || !string.IsNullOrWhiteSpace(AddCulture) && AcquisitionCulture is null) return true;
        if (item is not ProducerFromViewModel producer) return false;
        var selectedSorts = AcquisitionSorts;
        if (selectedSorts.Length > 0) return selectedSorts.Any(sort => sort.ProducerId == producer.Id);
        if (!string.IsNullOrWhiteSpace(AddSort)) return true;
        var linked = _catalog.Sorts.Where(SortInScope).ToArray();
        return linked.Length == 0 || linked.Any(candidate => candidate.ProducerId == producer.Id);
    }

    private void OnAcquisitionClassChanged() => UpdateAcquisition(true);
    private void OnAcquisitionCultureChanged() => UpdateAcquisition(false);
    private void OnAcquisitionSortChanged() => UpdateAcquisition(false);

    private void UpdateAcquisition(bool classChanged)
    {
        if (_updatingAcquisition) return;
        if (_catalog is null) { RefreshAcquisitionViews(); return; }
        _updatingAcquisition = true;
        var cleared = false;
        try
        {
            if (AcquisitionCulture is { } culture)
            {
                if (classChanged && culture.Class != AddClass) { AddCulture = string.Empty; cleared = true; }
                else AddClass = culture.Class;
            }
            if (!string.IsNullOrEmpty(AddSort) && _catalog.Sorts.Any(sort => SameAcquisitionName(sort.Name, AddSort)) && AcquisitionSorts.Length == 0)
            { AddSort = string.Empty; cleared = true; }
            var producers = _catalog.Producers.Where(producer =>
                SameAcquisitionName(producer.Name, AddProducer)).ToArray();
            if (!IsCollected && !string.IsNullOrEmpty(AddProducer) && producers.Length > 0
                && !producers.Any(producer => MatchesAcquisitionProducer(
                    new ProducerFromViewModel { Id = producer.Id, Name = producer.Name })))
            { AddProducer = string.Empty; cleared = true; }
            AcquisitionSelectionHint = cleared ? "Несовместимые сорт или производитель сброшены. Проверьте выбор." : string.Empty;
            OnPropertyChanged(nameof(AcquisitionSelectionHint));
            RefreshAcquisitionViews();
        }
        finally { _updatingAcquisition = false; }
    }

    private void RefreshAcquisitionViews()
    {
        CultureListView?.Refresh(); SortListView?.Refresh(); ProducerListView?.Refresh();
    }

    private void UpdateCatalogSort(SortDetailsFromViewModel updated)
    {
        if (_catalog is null) return;
        _catalog = _catalog with { Sorts = _catalog.Sorts.Select(sort => sort.Id == updated.Id
            ? sort with { Name = updated.Name, CultureIds = updated.Plants.Select(plant => plant.Culture.Id).Distinct().ToImmutableArray() } : sort).ToImmutableArray() };
        RefreshAcquisitionViews();
    }
    private void UpdateCatalogCulture(CultureDetailsFromViewModel updated)
    {
        if (_catalog is null) return;
        _catalog = _catalog with { Cultures = _catalog.Cultures.Select(culture => culture.Id == updated.Id ? updated : culture).ToImmutableArray() };
        RefreshAcquisitionViews();
    }
    private void UpdateCatalogProducer(ProducerDetailsFromViewModel updated)
    {
        if (_catalog is null) return;
        _catalog = _catalog with { Producers = _catalog.Producers.Select(producer => producer.Id == updated.Id ? updated : producer).ToImmutableArray() };
        RefreshAcquisitionViews();
    }

    private void RebuildCatalogSuggestions()
    {
        if (_catalog is null) return;
        ReconcileChoices(AddCultureList, _catalog.Cultures.OrderBy(culture => culture.Name)
            .Select(culture => new CultureFromViewModel { Id = culture.Id, Name = culture.Name }), item => item.Id, item => item.Name);
        ReconcileChoices(AddSortList, _catalog.Sorts.OrderBy(sort => sort.Name)
            .Select(sort => new SortFromSeedsViewModel { Id = sort.Id, Name = sort.Name }), item => item.Id, item => item.Name);
        ReconcileChoices(AddProducerList, _catalog.Producers.OrderBy(producer => producer.Name)
            .Select(producer => new ProducerFromViewModel { Id = producer.Id, Name = producer.Name }), item => item.Id, item => item.Name);
        _cultureListView.Source = AddCultureList; _sortListView.Source = AddSortList; _producerListView.Source = AddProducerList;
        ListCulture = ["-Выбрать все-", .. _catalog.Cultures.Select(c => c.Name).Distinct().OrderBy(n => n)];
        OnPropertyChanged(nameof(CultureListView)); OnPropertyChanged(nameof(SortListView)); OnPropertyChanged(nameof(ProducerListView));
        OnPropertyChanged(nameof(ListCulture));
        RefreshAcquisitionViews();
    }

    private static void ReconcileChoices<T>(ObservableCollection<T> current, IEnumerable<T> desired,
        Func<T, int> id, Func<T, string?> name) where T : class
    {
        var values = desired.ToArray();
        var ids = values.Select(id).ToHashSet();
        for (var index = current.Count - 1; index >= 0; index--)
            if (!ids.Contains(id(current[index]))) current.RemoveAt(index);
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            var existing = current.FirstOrDefault(item => id(item) == id(value));
            if (existing is null) current.Insert(index, value);
            else
            {
                var previous = current.IndexOf(existing);
                if (previous != index) current.Move(previous, index);
                if (name(existing) != name(value)) current[index] = value;
            }
        }
    }

    public async Task<Result<bool>> RefreshLibrariesAsync()
    {
        var options = await ReloadEditorOptionsAsync();
        if (options.IsFailure) return options;
        var dirty = HasUnsavedSeedChanges;
        var id = SelectedItem?.Id;
        return (await RefreshSeedsAsync()).Bind(_ =>
        {
            if (!dirty && id is { } selected) SelectedItem = Seeds?.FirstOrDefault(seed => seed.Id == selected);
            return Result.Success(true);
        });
    }
}
