using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Data;
using Bonfire.Application.Libraries;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public class LibraryEditorViewModel(ILibraryService libraryService, IUserDialog userDialog, SeedsViewModel seedsViewModel, PlanningLibrariesViewModel planningLibraries)
    : ViewModel
{
    public PlanningLibrariesViewModel PlanningLibraries { get; } = planningLibraries;
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
        if (SelectedSort?.Id != id) return;
        result.Match(sort =>
        {
            SortDetail = LibraryMapper.ToEditModel(sort);
            return true;
        }, error =>
        {
            _userDialog.Error(error);
            return false;
        });
    }

    private async Task LoadCultureDetailAsync(int id)
    {
        var result = await _libraryService.GetCultureAsync(id);
        if (SelectedCulture?.Id != id) return;
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
        if (SelectedProducer?.Id != id) return;
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

    // Команды — Сорт

    [field: AllowNull, MaybeNull]
    public ICommand SaveSortCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            try
            {
                if (SortDetail is not { } detail) return;
                var result = await _libraryService.UpdateSortAsync(detail);
                result.Match(saved =>
                {
                    _seedsViewModel.SyncAfterSortUpdate(saved);
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
                    _seedsViewModel.SyncAfterCultureUpdate(saved);
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
                    _seedsViewModel.SyncAfterProducerUpdate(saved);
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
