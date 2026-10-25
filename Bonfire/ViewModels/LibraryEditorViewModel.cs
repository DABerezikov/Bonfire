using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Data;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;

namespace Bonfire.ViewModels;

public class LibraryEditorViewModel(ILibraryService libraryService, IUserDialog userDialog, SeedsViewModel seedsViewModel)
    : ViewModel
{
    private readonly ILibraryService _libraryService = libraryService;
    private readonly IUserDialog _userDialog = userDialog;
    private readonly SeedsViewModel _seedsViewModel = seedsViewModel;
    private string? _originalProducerName;

    public IReadOnlyList<string> ClassList => PlantClassList.GetClassList();

    public ObservableCollection<SortFromSeedsViewModel> Sort => _seedsViewModel.AddSortList;
    public ObservableCollection<CultureFromViewModel> Culture => _seedsViewModel.AddCultureList;
    public ObservableCollection<ProducerFromViewModel> Producer => _seedsViewModel.AddProducerList;

    public SortFromSeedsViewModel? SelectedSort
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value != null) { SelectedCulture = null; SelectedProducer = null; }
            SortDetail = null;
            if (value != null)
                _ = LoadSortDetailAsync(value.Id);
        }
    }

    public CultureFromViewModel? SelectedCulture
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value != null) { SelectedSort = null; SelectedProducer = null; }
            CultureDetail = null;
            if (value != null)
                _ = LoadCultureDetailAsync(value.Id);
        }
    }

    public ProducerFromViewModel? SelectedProducer
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value != null) { SelectedSort = null; SelectedCulture = null; }
            _originalProducerName = null;
            ProducerDetailName = null;
            ProducerDetailIsDirty = false;
            if (value != null)
                _ = LoadProducerDetailAsync(value.Id);
        }
    }

    public SortEditModel? SortDetail
    {
        get;
        set => Set(ref field, value);
    }

    public CultureEditModel? CultureDetail
    {
        get;
        set => Set(ref field, value);
    }

    public string? ProducerDetailName
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            ProducerDetailIsDirty = value != _originalProducerName;
            OnPropertyChanged(nameof(ProducerDetailHasError));
        }
    }

    public bool ProducerDetailIsDirty
    {
        get;
        set => Set(ref field, value);
    }

    public bool ProducerDetailHasError => string.IsNullOrWhiteSpace(ProducerDetailName);

    // Загрузка деталей

    private async Task LoadSortDetailAsync(int id)
    {
        var result = await _libraryService.GetSortAsync(id);
        if (result.IsFailure) { _userDialog.Error(result.Error!); return; }
        SortDetail = LibraryMapper.ToEditModel(result.Value!);
    }

    private async Task LoadCultureDetailAsync(int id)
    {
        var result = await _libraryService.GetCultureAsync(id);
        result.Match(culture =>
        {
            CultureDetail = LibraryMapper.ToEditModel(culture);
            return true;
        }, error =>
        {
            _userDialog.Error(error);
            return false;
        });
    }

    private async Task LoadProducerDetailAsync(int id)
    {
        var result = await _libraryService.GetProducerAsync(id);
        result.Match(producer =>
        {
            _originalProducerName = producer.Name;
            ProducerDetailName = producer.Name;
            ProducerDetailIsDirty = false;
            return true;
        }, error =>
        {
            _userDialog.Error(error);
            return false;
        });
    }

    // Синхронизация с SeedsViewModel

    private void SyncAfterSortUpdate(PlantSort updated)
    {
        if (_seedsViewModel.Seeds != null)
            foreach (var seed in _seedsViewModel.Seeds.Where(s => s.Plant.PlantSort.Id == updated.Id))
            {
                seed.Plant.PlantSort.Name = updated.Name;
                var plant = updated.Plants.FirstOrDefault(p => p.Id == seed.Plant.Id);
                if (plant != null) seed.Plant.PlantCulture = plant.PlantCulture;
            }

        var sortItem = _seedsViewModel.AddSortList.FirstOrDefault(s => s.Id == updated.Id);
        if (sortItem is not null)
            _seedsViewModel.AddSortList[_seedsViewModel.AddSortList.IndexOf(sortItem)] =
                new SortFromSeedsViewModel { Id = updated.Id, Name = updated.Name };

        _seedsViewModel.UpdateCollectionViewSource();
    }

    private void SyncAfterCultureUpdate(PlantCulture updated)
    {
        if (_seedsViewModel.Seeds != null)
            foreach (var seed in _seedsViewModel.Seeds.Where(s => s.Plant.PlantCulture.Id == updated.Id))
            {
                seed.Plant.PlantCulture.Name = updated.Name;
                seed.Plant.PlantCulture.Class = updated.Class;
            }

        var cultureItem = _seedsViewModel.AddCultureList.FirstOrDefault(c => c.Id == updated.Id);
        if (cultureItem is not null)
            _seedsViewModel.AddCultureList[_seedsViewModel.AddCultureList.IndexOf(cultureItem)] =
                new CultureFromViewModel { Id = updated.Id, Name = updated.Name };

        _seedsViewModel.UpdateCollectionViewSource();
    }

    private void SyncAfterProducerUpdate(Producer updated)
    {
        if (_seedsViewModel.Seeds != null)
            foreach (var seed in _seedsViewModel.Seeds.Where(s => s.Plant.PlantSort.Producer.Id == updated.Id))
                seed.Plant.PlantSort.Producer.Name = updated.Name;

        var producerItem = _seedsViewModel.AddProducerList.FirstOrDefault(p => p.Id == updated.Id);
        if (producerItem is not null)
            _seedsViewModel.AddProducerList[_seedsViewModel.AddProducerList.IndexOf(producerItem)] =
                new ProducerFromViewModel { Id = updated.Id, Name = updated.Name };

        _seedsViewModel.UpdateCollectionViewSource();
    }
    
    // Команды — Сорт

    [field: AllowNull, MaybeNull]
    public ICommand SaveSortCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            try
            {
                var detail = SortDetail!;
                var result = await _libraryService.UpdateSortAsync(detail);
                if (result.IsFailure) { _userDialog.Error(result.Error!); return; }
                SyncAfterSortUpdate(result.Value!);
                detail.ResetDirty();
                _userDialog.Information("Сохранено");
            }
            catch (Exception ex)
            {
                _userDialog.Error($"Не удалось сохранить: {ex.Message}");
            }
        }, () => SortDetail is { HasErrors: false, IsDirty: true });

    [field: AllowNull, MaybeNull]
    public ICommand CancelSortCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            if (SelectedSort != null)
                await LoadSortDetailAsync(SelectedSort.Id);
        }, () => SortDetail is { IsDirty: true });

    // Команды — Культура

    [field: AllowNull, MaybeNull]
    public ICommand SaveCultureCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            try
            {
                if (CultureDetail is not { } detail) return;
                var result = await _libraryService.UpdateCultureAsync(detail);
                result.Match(saved =>
                {
                    SyncAfterCultureUpdate(saved);
                    detail.ResetDirty();
                    _userDialog.Information("Сохранено");
                    return true;
                }, error =>
                {
                    _userDialog.Error(error);
                    return false;
                });
            }
            catch (Exception ex)
            {
                _userDialog.Error($"Не удалось сохранить: {ex.Message}");
            }
        }, () => CultureDetail is { HasErrors: false, IsDirty: true });

    [field: AllowNull, MaybeNull]
    public ICommand CancelCultureCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            if (SelectedCulture != null)
                await LoadCultureDetailAsync(SelectedCulture.Id);
        }, () => CultureDetail is { IsDirty: true });

    // Команды — Производитель

    [field: AllowNull, MaybeNull]
    public ICommand SaveProducerCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            try
            {
                if (SelectedProducer is not { } producer || ProducerDetailName is not { } name) return;
                var result = await _libraryService.UpdateProducerAsync(producer.Id, name);
                result.Match(saved =>
                {
                    SyncAfterProducerUpdate(saved);
                    _originalProducerName = saved.Name;
                    ProducerDetailName = saved.Name;
                    ProducerDetailIsDirty = false;
                    _userDialog.Information("Сохранено");
                    return true;
                }, error =>
                {
                    _userDialog.Error(error);
                    return false;
                });
            }
            catch (Exception ex)
            {
                _userDialog.Error($"Не удалось сохранить: {ex.Message}");
            }
        }, () => SelectedProducer != null && !ProducerDetailHasError && ProducerDetailIsDirty);

    [field: AllowNull, MaybeNull]
    public ICommand CancelProducerCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            if (SelectedProducer != null)
                await LoadProducerDetailAsync(SelectedProducer.Id);
        }, () => SelectedProducer != null && ProducerDetailIsDirty);
}
