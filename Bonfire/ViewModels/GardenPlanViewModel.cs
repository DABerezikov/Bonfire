using Bonfire.Application.Seeds;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using Bonfire.Domain.Garden.SpotStates;
using Bonfire.Domain.Garden.States;

namespace Bonfire.ViewModels;

public class GardenPlanViewModel(
    IGardenService gardenService,
    IUserDialog userDialog,
    ISeedlingsService seedlingsService,
    ISeedsService seedsService,
    IPlantingService plantingService)
    : ViewModel
{
    public bool IsActive
    {
        get;
        set
        {
            field = value;
            // Семена/рассада могли измениться на других вкладках — при возврате
            // на планировщик обновляем пикеры посадки (без сброса выбора плана/участка).
            if (value && Plans.Count > 0)
                _ = LoadPlantLabelsAsync();
        }
    }

    // --- Планы ---

    public ObservableCollection<GardenPlanFromViewModel> Plans
    {
        get;
        set => Set(ref field, value);
    } = [];

    public GardenPlanFromViewModel? SelectedPlan
    {
        get;
        set
        {
            if (Set(ref field, value))
                _ = LoadGardensAsync();
        }
    }

    // --- Участки текущего плана ---

    public ObservableCollection<GardenFromViewModel> Gardens
    {
        get;
        set => Set(ref field, value);
    } = [];

    public GardenFromViewModel? SelectedGarden
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            SelectedElement = null;
            GardenEditor = value is null ? null : GardenEditorMapper.Open(value);
            OnPropertyChanged(nameof(GardenPropertiesPanel));
            if (value is not null) RecalculateInitialZoom();
        }
    }

    public GardenEditorDraft? GardenEditor
    {
        get;
        private set
        {
            if (Set(ref field, value)) GardenEditorHasBindingErrors = false;
        }
    }

    public bool GardenEditorHasBindingErrors { get; set; }
    private bool _isSavingGardenProperties;
    public bool IsGardenEditorEnabled => !_isSavingGardenProperties;

    // ─── Масштаб холста ───

    private double _gardenZoom = 1.0;
    public double GardenZoom
    {
        get => _gardenZoom;
        set
        {
            _gardenZoom = Math.Max(0.1, Math.Min(5.0, Math.Round(value, 2)));
            OnPropertyChanged();
            OnPropertyChanged(nameof(GardenZoomPercent));
            UpdateElementZoom();
        }
    }

    public string GardenZoomPercent => $"{_gardenZoom * 100:F0}%";

    [field: AllowNull, MaybeNull]
    public ICommand ZoomInCommand    => field ??= new LambdaCommand(() => GardenZoom += 0.1);

    [field: AllowNull, MaybeNull]
    public ICommand ZoomOutCommand   => field ??= new LambdaCommand(() => GardenZoom -= 0.1);
    [field: AllowNull, MaybeNull]
    public ICommand ResetZoomCommand => field ??= new LambdaCommand(RecalculateInitialZoom);

    private void UpdateElementZoom()
    {
        if (SelectedGarden is null) return;
        foreach (var el in SelectedGarden.Elements)
            el.CanvasZoom = _gardenZoom;
        foreach (var gh in SelectedGarden.Greenhouses)
        {
            gh.CanvasZoom = _gardenZoom;
            foreach (var el in gh.InnerElements)
                el.CanvasZoom = _gardenZoom;
        }
    }

    public void RecalculateInitialZoom()
    {
        if (SelectedGarden is null) { GardenZoom = 1.0; return; }
        const double targetPx = 80.0;
        var sizes = SelectedGarden.Elements
            .Select(e => Math.Min(e.Width, e.Height))
            .Concat(SelectedGarden.Greenhouses.Select(gh => Math.Min(gh.DisplayWidth, gh.DisplayHeight)));
        var smallest = sizes.DefaultIfEmpty(targetPx).Min();
        // Не уменьшаем ниже 100%: при масштабе 150 пкс/м элементы уже читаемы на 1.0
        GardenZoom = Math.Max(1.0, Math.Min(smallest > 0 ? targetPx / smallest : 1.0, 4.0));
    }

    // --- Выбранный элемент на холсте ---

    public GardenElementFromViewModel? SelectedElement
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            ElementEditor = value is null ? null : ElementEditorMapper.Open(value);
            OnPropertyChanged(nameof(GardenPropertiesPanel));
            OnPropertyChanged(nameof(GreenhousePropertiesPanel));
        }
    }

    public ElementEditorDraft? ElementEditor
    {
        get;
        private set
        {
            if (Set(ref field, value)) ElementEditorHasBindingErrors = false;
        }
    }

    public bool ElementEditorHasBindingErrors { get; set; }
    private bool _isSavingElementProperties;
    public bool IsElementEditorEnabled => !_isSavingElementProperties;

    /// <summary>
    /// Возвращает SelectedGarden, когда не выбран ни один элемент —
    /// используется для отображения панели свойств участка в правой колонке.
    /// </summary>
    public GardenFromViewModel? GardenPropertiesPanel =>
        SelectedGarden is not null && SelectedElement is null ? SelectedGarden : null;

    // --- Выбранная теплица (для второго уровня Canvas) ---

    public GreenhouseFromViewModel? SelectedGreenhouse
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            GreenhouseEditor = value is null ? null : GreenhouseEditorMapper.Open(value);
            SelectedElement = null;
            OnPropertyChanged(nameof(GreenhousePropertiesPanel));
        }
    }

    public GreenhouseEditorDraft? GreenhouseEditor
    {
        get;
        private set
        {
            if (Set(ref field, value)) GreenhouseEditorHasBindingErrors = false;
        }
    }

    public bool GreenhouseEditorHasBindingErrors { get; set; }
    private bool _isSavingGreenhouseProperties;
    public bool IsGreenhouseEditorEnabled => !_isSavingGreenhouseProperties;

    /// <summary>True, когда открыт оверлей внутреннего пространства теплицы.</summary>
    public bool IsEditingGreenhouse
    {
        get;
        private set => Set(ref field, value);
    }

    /// <summary>
    /// Возвращает SelectedGreenhouse, когда не выбран ни один элемент —
    /// используется для отображения панели свойств теплицы в оверлее.
    /// </summary>
    public GreenhouseFromViewModel? GreenhousePropertiesPanel =>
        SelectedGreenhouse is not null && SelectedElement is null ? SelectedGreenhouse : null;

    // --- Абстракция «активный контейнер» ---
    // При редактировании теплицы операции работают с её внутренним пространством.

    private int ActiveContainerId =>
        SelectedGreenhouse?.Id ?? SelectedGarden?.Id ?? 0;

    private ObservableCollection<GardenElementFromViewModel> ActiveElements =>
        SelectedGreenhouse?.InnerElements ?? SelectedGarden?.Elements
        ?? [];

    private double ActiveCanvasWidth =>
        SelectedGreenhouse?.InnerCanvasWidth ?? SelectedGarden?.CanvasWidth ?? 0;

    private double ActiveCanvasHeight =>
        SelectedGreenhouse?.InnerCanvasHeight ?? SelectedGarden?.CanvasHeight ?? 0;

    private ObservableCollection<GreenhouseFromViewModel> ActiveGreenhouses =>
        SelectedGreenhouse is null
            ? (SelectedGarden?.Greenhouses
               ?? [])
            : [];

    // --- Тип добавляемого элемента ---

    public string SelectedElementType
    {
        get;
        set => Set(ref field, value);
    } = "Bed";

    // --- Сетка посадок ---

    public GardenElementFromViewModel? EditingGridElement
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsGridOpen
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>Выбранная ячейка в редакторе сетки посадок.</summary>
    public PlantingSpotFromViewModel? SelectedSpot
    {
        get;
        set
        {
            if (field is not null) field.IsSelected = false;
            if (Set(ref field, value) && value is not null)
                value.IsSelected = true;
        }
    }

    /// <summary>Дата посадки — по умолчанию сегодня.</summary>
    public DateTime NewPlantingDate
    {
        get;
        set => Set(ref field, value);
    } = DateTime.Today;

    // --- Планирование посадок (Запланировать) ---

    /// <summary>
    /// Список меток растений (культура + сорт) без количества.
    /// Используется как источник для ComboBox при планировании ячейки — без расхода инвентаря.
    /// </summary>
    public ObservableCollection<string> AvailablePlantLabels
    {
        get;
        set => Set(ref field, value);
    } = [];

    /// <summary>
    /// Растение, выбранное (или введённое вручную) для планирования.
    /// Сохраняется в PlantingSpot.Note без расхода инвентаря.
    /// </summary>
    public string PlanPickerLabel
    {
        get;
        set => Set(ref field, value);
    } = "";

    // --- Посадка (Посадить) ---

    /// <summary>Доступные партии рассады (Seedling.Quantity > 0).</summary>
    public ObservableCollection<PlantSourceItem> AvailableSeedlingItems
    {
        get;
        set => Set(ref field, value);
    } = [];

    /// <summary>Доступные пакеты семян (SeedsInfo.AmountSeeds > 0).</summary>
    public ObservableCollection<PlantSourceItem> AvailableSeedItems
    {
        get;
        set => Set(ref field, value);
    } = [];

    /// <summary>True — высаживаем рассаду; False — сеем семена напрямую.</summary>
    private bool _isFromSeedling = true;
    public bool IsFromSeedling
    {
        get => _isFromSeedling;
        set
        {
            if (value == _isFromSeedling) return;
            _isFromSeedling = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFromSeed));
            SelectedPlantSource = null;
        }
    }

    public bool IsFromSeed
    {
        get => !_isFromSeedling;
        set => IsFromSeedling = !value;
    }

    /// <summary>
    /// Выбранная позиция для посадки (рассада или семена).
    /// Пока null — кнопка «Посадить» задизаблена.
    /// </summary>
    public PlantSourceItem? SelectedPlantSource
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            PlantingAmount = 1;
            OnPropertyChanged(nameof(PlantingAmountUnit));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Сколько посадить/списать. Зажимается по доступному остатку источника,
    /// чтобы нельзя было посадить больше, чем есть в рассаде/семенах.</summary>
    public double PlantingAmount
    {
        get;
        set
        {
            var max = SelectedPlantSource?.AvailableQty ?? double.MaxValue;
            var clamped = value > 0 ? value : 1;
            if (clamped > max) clamped = max;
            Set(ref field, clamped);
        }
    } = 1;

    /// <summary>Единица измерения выбранного источника: «г.» или «шт.»</summary>
    public string PlantingAmountUnit =>
        SelectedPlantSource?.IsWeightBased == true ? Units.GramAbbr : Units.PiecesAbbr;

    // ─────────────────────────────────────
    //  Загрузка данных
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand LoadDataCommand => field
        ??= new LambdaCommandAsync(LoadDataAsync);

    private async Task LoadDataAsync()
    {
        var result = await gardenService.ReadPlansAsync();
        if (result.IsFailure) { userDialog.Warning(result.Match(_ => "", e => e), "Загрузка планов"); return; }
        Plans = new(result.ValueOrThrow().Select(GardenPlanMapper.MapPlan));
        SelectedPlan = Plans.FirstOrDefault();

        await LoadPlantLabelsAsync();
    }

    private async Task LoadPlantLabelsAsync()
    {
        // ── Рассада: из рассады сажают ВЗОШЕДШИЕ живые ростки штучно. ───────
        // Доступно = взошедшие живые − уже высаженные (вес рассады не при чём).
        var allSeedlings = await seedlingsService.GetAllSeedlingsAsync();

        AvailableSeedlingItems = new ObservableCollection<PlantSourceItem>(
            allSeedlings
            .Where(s => !s.IsPlantedInBed) // уже высаженные в грядку партии повторно не сажаем
            .Select(s =>
            {
                var name = $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}";
                var qty  = Services.SeedlingAvailability.Available(s);
                return new PlantSourceItem
                {
                    Kind          = PlantSourceKind.Seedling,
                    EntityId      = s.Id,
                    PlantName     = name,
                    Label         = $"{name} ({qty} шт.)",
                    IsWeightBased = false,
                    AvailableQty  = qty
                };
            })
            .Where(i => i.AvailableQty > 0)
            .OrderBy(i => i.PlantName));

        // ── Семена: штучные (AmountSeeds > 0) и граммовые (AmountSeedsWeight > 0) ──
        var seedResult = await seedsService.ReadSeedsAsync();
        if (seedResult.IsFailure) { userDialog.Warning(seedResult.Match(_ => "", e => e), "Загрузка семян"); return; }
        var seedEntities = seedResult.ValueOrThrow()
            .Where(s => s.SeedsInfo.AmountSeeds > 0 || s.SeedsInfo.AmountSeedsWeight.ToNullable() > 0)
            .ToList();

        AvailableSeedItems = new ObservableCollection<PlantSourceItem>(
            seedEntities
            .Select(s =>
            {
                var name        = $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}";
                var weightBased = s.SeedsInfo.AmountSeeds <= 0 && s.SeedsInfo.AmountSeedsWeight.ToNullable() > 0;
                var qty         = weightBased ? (s.SeedsInfo.AmountSeedsWeight.ToNullable() ?? 0) : s.SeedsInfo.AmountSeeds;
                var qtyLabel    = weightBased ? $"{qty:F1} г." : $"{(int)qty} шт.";
                return new PlantSourceItem
                {
                    Kind          = PlantSourceKind.Seed,
                    EntityId      = s.Id,
                    PlantName     = name,
                    Label         = $"{name} ({qtyLabel})",
                    IsWeightBased = weightBased,
                    AvailableQty  = qty
                };
            })
            .OrderBy(i => i.PlantName));

        // ── Метки для планирования (без количества, для свободного ввода) ───
        var planLabels = allSeedlings
            .Select(s => $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}")
            .Concat(seedEntities.Select(s => $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}"))
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        AvailablePlantLabels = new ObservableCollection<string>(planLabels);
    }

    private async Task LoadGardensAsync()
    {
        var plan = SelectedPlan;
        if (plan is null)
        {
            foreach (var garden in Gardens) CloseEditorsFor(garden);
            Gardens = [];
            SelectedGarden = null;
            return;
        }
        var result = await gardenService.ReadGardensAsync(plan.Id);
        if (!ReferenceEquals(SelectedPlan, plan)) return;
        if (result.IsFailure) { userDialog.Warning(result.Match(_ => "", e => e), "Загрузка участков"); return; }
        foreach (var garden in Gardens) CloseEditorsFor(garden);
        Gardens = new(result.ValueOrThrow().Select(GardenPlanMapper.MapGarden));
        SelectedGarden = Gardens.FirstOrDefault();
    }

    // ─────────────────────────────────────
    //  Управление планами
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand CreatePlanCommand => field
        ??= new LambdaCommandAsync(CreatePlanAsync);

    private async Task CreatePlanAsync()
    {
        var plans = Plans;
        var selected = SelectedPlan;
        var result = await gardenService.CreateCurrentPlanAsync();
        result.Match(saved =>
        {
            var plan = GardenPlanMapper.MapPlan(saved);
            plans.Insert(0, plan);
            if (ReferenceEquals(Plans, plans) && ReferenceEquals(SelectedPlan, selected)) SelectedPlan = plan;
            return true;
        }, error => { userDialog.Warning(error, "Создание плана"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand DeletePlanCommand => field
        ??= new LambdaCommandAsync(DeletePlanAsync,
            () => SelectedPlan is not null);

    private async Task DeletePlanAsync()
    {
        if (SelectedPlan is not { } plan) return;
        var plans = Plans;
        var gardens = Gardens;
        if (!userDialog.YesNoQuestion(
                $"Удалить план «{plan.Name}»? Все участки и элементы будут удалены.",
                "Удаление плана")) return;

        var result = await gardenService.DeletePlanByIdAsync(new(plan.Id));
        result.Match(_ =>
        {
            plans.Remove(plan);
            foreach (var garden in gardens) CloseEditorsFor(garden);
            if (ReferenceEquals(SelectedPlan, plan))
            {
                SelectedGarden = null;
                SelectedPlan = Plans.FirstOrDefault();
            }
            return true;
        }, error => { userDialog.Warning(error, "Удаление плана"); return false; });
    }

    // ─────────────────────────────────────
    //  Управление участком
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand CreateGardenCommand => field
        ??= new LambdaCommandAsync(CreateGardenAsync,
            () => SelectedPlan is not null);

    private async Task CreateGardenAsync()
    {
        if (SelectedPlan is not { } plan) return;
        var gardens = Gardens;
        var selected = SelectedGarden;
        var result = await gardenService.CreateGardenFromRequestAsync(new(
            plan.Id, "Новый участок", 20, 15, CanvasConstants.PixelsPerMeter));
        result.Match(saved =>
        {
            var garden = GardenPlanMapper.MapGarden(saved);
            gardens.Add(garden);
            if (ReferenceEquals(SelectedPlan, plan) && ReferenceEquals(Gardens, gardens) && ReferenceEquals(SelectedGarden, selected))
                SelectedGarden = garden;
            return true;
        }, error => { userDialog.Warning(error, "Создание участка"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand DeleteGardenCommand => field
        ??= new LambdaCommandAsync(DeleteGardenAsync,
            () => SelectedGarden is not null);

    private async Task DeleteGardenAsync()
    {
        if (SelectedGarden is not { } garden) return;
        var gardens = Gardens;
        if (!userDialog.YesNoQuestion(
                $"Удалить участок «{garden.Name}»? Все элементы и посадки будут удалены.",
                "Удаление участка")) return;

        var result = await gardenService.DeleteGardenByIdAsync(new(garden.Id));
        result.Match(_ =>
        {
            gardens.Remove(garden);
            CloseEditorsFor(garden);
            if (ReferenceEquals(SelectedGarden, garden)) SelectedGarden = Gardens.FirstOrDefault();
            return true;
        }, error => { userDialog.Warning(error, "Удаление участка"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand SaveGardenSizeCommand => field
        ??= new LambdaCommandAsync(SaveGardenSizeAsync,
            () => GardenEditor is { IsDirty: true } && !GardenEditorHasBindingErrors && !_isSavingGardenProperties);

    [field: AllowNull, MaybeNull]
    public ICommand CancelGardenPropertiesCommand => field
        ??= new LambdaCommand(() =>
        {
            if (SelectedGarden is { } garden) GardenEditor = GardenEditorMapper.Open(garden);
        }, () => SelectedGarden is not null && !_isSavingGardenProperties);

    private async Task SaveGardenSizeAsync()
    {
        if (SelectedGarden is not { } garden || GardenEditor is not { IsDirty: true } draft ||
            draft.Id != garden.Id || GardenEditorHasBindingErrors || _isSavingGardenProperties) return;
        var submitted = draft.Values;
        _isSavingGardenProperties = true;
        OnPropertyChanged(nameof(IsGardenEditorEnabled));
        try
        {
            var result = await gardenService.EditGardenPropertiesAsync(new(draft.Id, draft.Original, submitted, CanvasConstants.PixelsPerMeter));
            result.Match(saved =>
            {
                GardenEditorMapper.Apply(saved, garden);
                if (ReferenceEquals(SelectedGarden, garden) && GardenEditor is { } activeDraft)
                    activeDraft.Accept(saved.Values, ReferenceEquals(activeDraft, draft) ? submitted : activeDraft.Original);
                return true;
            }, error => { userDialog.Warning(error, "Изменение размера участка"); return false; });
        }
        finally
        {
            _isSavingGardenProperties = false;
            OnPropertyChanged(nameof(IsGardenEditorEnabled));
        }
    }

    // ─────────────────────────────────────
    //  Выделение и навигация
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand SelectElementCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is not GardenElementFromViewModel vm) return;

            SelectedElement?.IsSelected = false;
            SelectedElement = vm;
            vm.IsSelected = true;
        });

    /// <summary>
    /// Снимает выделение с элемента. Вызывается при клике по пустому месту холста или нажатии Escape.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public ICommand DeselectElementCommand => field
        ??= new LambdaCommand(() =>
        {
            SelectedElement?.IsSelected = false;
            SelectedElement = null;
        });

    [field: AllowNull, MaybeNull]
    public ICommand SetElementTypeCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is string type)
                SelectedElementType = type;
        });

    // Выделяет теплицу (подсветка + панель свойств) без открытия оверлея.
    [field: AllowNull, MaybeNull]
    public ICommand SelectGreenhouseCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is not GreenhouseFromViewModel gh) return;

            SelectedGreenhouse?.IsSelected = false;
            SelectedGreenhouse = gh;
            gh.IsSelected = true;
            // IsEditingGreenhouse остаётся false — оверлей не открывается
        });

    // Открывает оверлей внутреннего пространства теплицы (двойной клик).
    [field: AllowNull, MaybeNull]
    public ICommand OpenGreenhouseCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is not GreenhouseFromViewModel gh) return;
            SelectedGreenhouse?.IsSelected = false;
            SelectedGreenhouse = gh;
            gh.IsSelected = true;
            IsEditingGreenhouse = true;
        });

    [field: AllowNull, MaybeNull]
    public ICommand CloseGreenhouseCommand => field
        ??= new LambdaCommand(() =>
        {
            IsEditingGreenhouse = false;
            SelectedGreenhouse?.IsSelected = false;
            SelectedGreenhouse = null;
        });

    [field: AllowNull, MaybeNull]
    public ICommand ChangeGreenhouseStateCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeGreenhouseStateAsync(p as string),
            _ => SelectedGreenhouse is not null);

    private async Task ChangeGreenhouseStateAsync(string? stateTypeName)
    {
        if (SelectedGreenhouse is not { } editing || stateTypeName is null) return;

        var newState = GardenElementState.From(stateTypeName);
        var currentState = GardenElementState.From(editing.StateTypeName);
        if (!currentState.CanTransitionTo(newState)) return;

        var result = await gardenService.ChangeStateAsync(new(editing.Id, new StateChangeTarget.Greenhouse(newState)));
        ApplyStateResult(result, saved =>
        {
            var state = saved switch
            {
                GardenStateResultFromViewModel.Greenhouse greenhouse => greenhouse.State,
                _ => throw new System.Diagnostics.UnreachableException()
            };
            editing.StateTypeName = state.GetType().Name;
            editing.StateDisplayName = state.DisplayName;
            editing.StateColor = state.StatusColor;
        });
    }

    [field: AllowNull, MaybeNull]
    public ICommand DeleteGreenhouseCommand => field
        ??= new LambdaCommandAsync(DeleteGreenhouseAsync,
            () => SelectedGreenhouse is not null);

    private async Task DeleteGreenhouseAsync()
    {
        if (SelectedGreenhouse is not { } house || SelectedGarden is not { } garden) return;
        var houses = garden.Greenhouses;
        if (!userDialog.YesNoQuestion(
                $"Удалить теплицу «{house.Name}»? Все элементы и посадки будут удалены.",
                "Удаление теплицы")) return;

        var result = await gardenService.DeleteGreenhouseByIdAsync(new(house.Id));
        result.Match(_ =>
        {
            houses.Remove(house);
            if (EditingGridElement is { } grid && house.InnerElements.Contains(grid)) CloseGridFor(grid);
            if (ReferenceEquals(SelectedGreenhouse, house))
            {
                IsEditingGreenhouse = false;
                SelectedGreenhouse = null;
            }
            else if (SelectedElement is { } selected && house.InnerElements.Contains(selected)) SelectedElement = null;
            return true;
        }, error => { userDialog.Warning(error, "Удаление теплицы"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand SaveGreenhouseSizeCommand => field
        ??= new LambdaCommandAsync(SaveGreenhouseSizeAsync,
            () => GreenhouseEditor is { IsDirty: true } && !GreenhouseEditorHasBindingErrors && !_isSavingGreenhouseProperties);

    [field: AllowNull, MaybeNull]
    public ICommand CancelGreenhousePropertiesCommand => field
        ??= new LambdaCommand(() =>
        {
            if (SelectedGreenhouse is { } house) GreenhouseEditor = GreenhouseEditorMapper.Open(house);
        }, () => SelectedGreenhouse is not null && !_isSavingGreenhouseProperties);

    private async Task SaveGreenhouseSizeAsync()
    {
        if (SelectedGreenhouse is not { } house || GreenhouseEditor is not { IsDirty: true } draft ||
            draft.Id != house.Id || GreenhouseEditorHasBindingErrors || _isSavingGreenhouseProperties) return;
        var submitted = draft.Values;
        _isSavingGreenhouseProperties = true;
        OnPropertyChanged(nameof(IsGreenhouseEditorEnabled));
        try
        {
            var result = await gardenService.EditGreenhousePropertiesAsync(new(draft.Id, draft.Original, submitted, CanvasConstants.PixelsPerMeter));
            result.Match(saved =>
            {
                GreenhouseEditorMapper.Apply(saved, house);
                if (ReferenceEquals(SelectedGreenhouse, house) && GreenhouseEditor is { } activeDraft)
                    activeDraft.Accept(saved.Values, ReferenceEquals(activeDraft, draft) ? submitted : activeDraft.Original);
                return true;
            }, error => { userDialog.Warning(error, "Изменение размера теплицы"); return false; });
        }
        finally
        {
            _isSavingGreenhouseProperties = false;
            OnPropertyChanged(nameof(IsGreenhouseEditorEnabled));
        }
    }

    // ─────────────────────────────────────
    //  Добавление элементов
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand AddElementCommand => field
        ??= new LambdaCommandAsync(AddElementAsync,
            () => SelectedGarden is not null);

    private async Task AddElementAsync()
    {
        if (SelectedGarden is null) return;

        var activeElements    = ActiveElements;
        var activeGreenhouses = ActiveGreenhouses;
        var canvasW = ActiveCanvasWidth;
        var canvasH = ActiveCanvasHeight;

        var garden = SelectedGarden;
        var greenhouse = SelectedGreenhouse;
        var selected = SelectedElement;
        var containerId = ActiveContainerId;
        var template = GardenElementTemplate.From(SelectedElementType);
        var defaultW = template.WidthMeters * CanvasConstants.PixelsPerMeter;
        var defaultH = template.HeightMeters * CanvasConstants.PixelsPerMeter;

        var placement = CollisionHelper.FindPlacement(
            activeElements, activeGreenhouses, exclude: null,
            defaultW, defaultH, canvasW, canvasH);

        if (placement.IsFailure)
        {
            userDialog.Warning(placement.Match(_ => "", error => error), "Добавление элемента");
            return;
        }
        var spot = placement.ValueOrThrow().ToNullable();

        if (spot is null)
        {
            var containerName = SelectedGreenhouse?.Name ?? SelectedGarden.Name;
            userDialog.Warning(
                $"В «{containerName}» нет свободного места для нового элемента.\n" +
                "Увеличьте размер или уберите лишние элементы.",
                "Добавление элемента");
            return;
        }

        var result = await gardenService.CreateElementFromRequestAsync(new(containerId,
            $"{template.Label} {activeElements.Count + 1}", template.Kind, spot.Value.x, spot.Value.y, defaultW, defaultH));
        result.Match(saved =>
        {
            var model = GardenPlanMapper.MapElement(saved);
            model.ContainerElements = activeElements;
            model.ContainerGreenhouses = activeGreenhouses;
            model.CanvasZoom = _gardenZoom;
            activeElements.Add(model);
            if (ReferenceEquals(SelectedGarden, garden) && ReferenceEquals(SelectedGreenhouse, greenhouse) && ReferenceEquals(SelectedElement, selected))
            {
                if (Math.Min(defaultW, defaultH) * _gardenZoom < 64) RecalculateInitialZoom();
                SelectedElement = model;
            }
            return true;
        }, error => { userDialog.Warning(error, "Добавление элемента"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand AddGreenhouseCommand => field
        ??= new LambdaCommandAsync(AddGreenhouseAsync,
            () => SelectedGarden is not null);

    private async Task AddGreenhouseAsync()
    {
        if (SelectedGarden is not { } garden) return;

        var ghScale = CanvasConstants.PixelsPerMeter;
        double ghW = 6 * ghScale, ghH = 3 * ghScale; // 6×3 м

        var placement = CollisionHelper.FindPlacement(
            garden.Elements, garden.Greenhouses, exclude: null,
            ghW, ghH, garden.CanvasWidth, garden.CanvasHeight);

        if (placement.IsFailure)
        {
            userDialog.Warning(placement.Match(_ => "", error => error), "Добавление теплицы");
            return;
        }
        var spot = placement.ValueOrThrow().ToNullable();

        if (spot is null)
        {
            userDialog.Warning(
                $"На участке «{garden.Name}» нет свободного места для теплицы 6×3 м.\n" +
                "Увеличьте участок или уберите лишние элементы.",
                "Добавление теплицы");
            return;
        }

        var result = await gardenService.CreateGreenhouseFromRequestAsync(new(garden.Id,
            $"Теплица {garden.Greenhouses.Count + 1}", 6, 3, ghScale, spot.Value.x, spot.Value.y));
        result.Match(saved =>
        {
            var house = GardenPlanMapper.MapGreenhouse(saved);
            house.ContainerElements = garden.Elements;
            house.ContainerGreenhouses = garden.Greenhouses;
            house.CanvasZoom = _gardenZoom;
            garden.Greenhouses.Add(house);
            return true;
        }, error => { userDialog.Warning(error, "Добавление теплицы"); return false; });
    }

    // ─────────────────────────────────────
    //  Удаление / перемещение элементов
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand DeleteElementCommand => field
        ??= new LambdaCommandAsync(DeleteElementAsync,
            () => SelectedElement is not null);

    private async Task DeleteElementAsync()
    {
        if (SelectedElement is not { } editing || SelectedGarden is null) return;
        var elements = ActiveElements;
        var result = await gardenService.DeleteGardenElementAsync(new(editing.Id));
        result.Match(_ =>
        {
            elements.Remove(editing);
            if (ReferenceEquals(SelectedElement, editing)) SelectedElement = null;
            CloseGridFor(editing);
            return true;
        }, error => { userDialog.Warning(error, "Удаление элемента"); return false; });
    }

    private void CloseGridFor(GardenElementFromViewModel model)
    {
        if (!ReferenceEquals(EditingGridElement, model)) return;
        IsGridOpen = false;
        EditingGridElement = null;
        SelectedSpot = null;
    }

    private void CloseEditorsFor(GardenFromViewModel garden)
    {
        if (EditingGridElement is { } grid &&
            (garden.Elements.Contains(grid) || garden.Greenhouses.Any(h => h.InnerElements.Contains(grid))))
            CloseGridFor(grid);
        if (SelectedGreenhouse is { } house && garden.Greenhouses.Contains(house))
        {
            IsEditingGreenhouse = false;
            SelectedGreenhouse = null;
        }
    }

    [field: AllowNull, MaybeNull]
    public ICommand ToggleLockElementCommand => field
        ??= new LambdaCommandAsync(
            async p => await ToggleLockElementAsync(p as GardenElementFromViewModel),
            _ => true);

    private Task ToggleLockElementAsync(GardenElementFromViewModel? model)
        => model is null ? Task.CompletedTask : SetGardenLockAsync(model.Id, new GardenLockTarget.Element(), !model.IsLocked,
            locked => { if (model.IsLocked != locked) model.IsLocked = locked; });

    [field: AllowNull, MaybeNull]
    public ICommand ToggleLockGreenhouseCommand => field
        ??= new LambdaCommandAsync(
            async p => await ToggleLockGreenhouseAsync(p as GreenhouseFromViewModel),
            _ => true);

    private Task ToggleLockGreenhouseAsync(GreenhouseFromViewModel? model)
        => model is null ? Task.CompletedTask : SetGardenLockAsync(model.Id, new GardenLockTarget.Greenhouse(), !model.IsLocked,
            locked => { if (model.IsLocked != locked) model.IsLocked = locked; });

    private async Task SetGardenLockAsync(int id, GardenLockTarget target, bool isLocked, Action<bool> apply)
    {
        var result = await gardenService.SetLockAsync(new(id, target, isLocked));
        result.Match(saved => { apply(saved.IsLocked); return true; },
            error => { userDialog.Warning(error, "Блокировка геометрии"); return false; });
    }

    [field: AllowNull, MaybeNull]
    public ICommand SaveElementPositionCommand => field
        ??= new LambdaCommandAsync(async parameter =>
        {
            if ((parameter as GardenElementFromViewModel ?? SelectedElement) is { IsLocked: false } model)
                await SaveElementGeometryAsync(model);
        }, parameter => (parameter as GardenElementFromViewModel ?? SelectedElement) is { IsLocked: false });

    private Task SaveElementGeometryAsync(GardenElementFromViewModel model)
        => SaveGeometryAsync(new(model.Id, new GeometryTarget.Element(CanvasConstants.PixelsPerMeter), model.X, model.Y, model.Width, model.Height),
            rectangle =>
            {
                CanvasGeometryMapper.Apply(rectangle, model);
                if (ReferenceEquals(SelectedElement, model) && ElementEditor is { } draft && !ElementEditorHasBindingErrors)
                    draft.Accept(ElementEditorMapper.Read(model), draft.Original);
            });

    [field: AllowNull, MaybeNull]
    public ICommand SaveElementPropertiesCommand => field
        ??= new LambdaCommandAsync(SaveElementPropertiesAsync,
            () => ElementEditor is { IsDirty: true } && !ElementEditorHasBindingErrors && !_isSavingElementProperties);

    [field: AllowNull, MaybeNull]
    public ICommand CancelElementPropertiesCommand => field
        ??= new LambdaCommand(() =>
        {
            if (SelectedElement is { } element) ElementEditor = ElementEditorMapper.Open(element);
        }, () => SelectedElement is not null && !_isSavingElementProperties);

    private async Task SaveElementPropertiesAsync()
    {
        if (SelectedElement is not { } element || ElementEditor is not { IsDirty: true } draft ||
            draft.Id != element.Id || ElementEditorHasBindingErrors || _isSavingElementProperties) return;
        var submitted = draft.Values;
        _isSavingElementProperties = true;
        OnPropertyChanged(nameof(IsElementEditorEnabled));
        try
        {
            var result = await gardenService.EditElementPropertiesAsync(new(draft.Id, draft.Original, submitted, CanvasConstants.PixelsPerMeter));
            result.Match(saved =>
            {
                ElementEditorMapper.Apply(saved, element);
                if (ReferenceEquals(SelectedElement, element) && ElementEditor is { } activeDraft)
                    activeDraft.Accept(ElementEditorMapper.Values(saved), ReferenceEquals(activeDraft, draft) ? submitted : activeDraft.Original);
                return true;
            }, error => { userDialog.Warning(error, "Свойства элемента"); return false; });
        }
        finally
        {
            _isSavingElementProperties = false;
            OnPropertyChanged(nameof(IsElementEditorEnabled));
        }
    }

    [field: AllowNull, MaybeNull]
    public ICommand SaveGreenhousePositionCommand => field
        ??= new LambdaCommandAsync(async parameter =>
        {
            if (parameter is GreenhouseFromViewModel { IsLocked: false } model)
                await SaveGreenhouseGeometryAsync(model);
        }, parameter => parameter is GreenhouseFromViewModel { IsLocked: false });

    private Task SaveGreenhouseGeometryAsync(GreenhouseFromViewModel model)
        => SaveGeometryAsync(new(model.Id, new GeometryTarget.Greenhouse(), model.X, model.Y, model.DisplayWidth, model.DisplayHeight),
            rectangle => CanvasGeometryMapper.Apply(rectangle, model));

    private async Task SaveGeometryAsync(SaveGardenGeometryFromViewModel request, Action<CanvasRectangle> apply)
    {
        var result = await gardenService.SaveGeometryAsync(request);
        result.Match(saved => { apply(saved.Rectangle); return true; },
            error => { userDialog.Warning(error, "Сохранение геометрии"); return false; });
    }

    // ─────────────────────────────────────
    //  Переходы состояний
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand ChangeElementStateCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeElementStateAsync(p as string),
            _ => SelectedElement is not null);

    private async Task ChangeElementStateAsync(string? stateTypeName)
    {
        if (SelectedElement is not { } editing || stateTypeName is null) return;

        // Проверяем допустимость перехода до обращения к БД — кнопка должна быть задизаблена,
        // но на случай гонки или вызова из ContextMenu дублируем проверку здесь.
        var newState = GardenElementState.From(stateTypeName);
        var currentState = GardenElementState.From(editing.StateTypeName);
        if (!currentState.CanTransitionTo(newState)) return;

        var result = await gardenService.ChangeStateAsync(new(editing.Id, new StateChangeTarget.Element(newState)));
        ApplyStateResult(result, saved =>
        {
            var state = saved switch
            {
                GardenStateResultFromViewModel.Element element => element.State,
                _ => throw new System.Diagnostics.UnreachableException()
            };
            editing.StateTypeName = state.GetType().Name;
            editing.StateDisplayName = state.DisplayName;
            editing.StateColor = state.StatusColor;
            editing.CanAddPlanting = state.CanAddPlanting;
            editing.CanModifyGrid = state.CanModifyGrid;
        });
    }

    // ─────────────────────────────────────
    //  Сетка посадок
    // ─────────────────────────────────────

    [field: AllowNull, MaybeNull]
    public ICommand OpenPlantingGridCommand => field
        ??= new LambdaCommand(() =>
        {
            EditingGridElement = SelectedElement;
            SelectedSpot       = null;
            IsGridOpen         = SelectedElement is not null;
        }, () => SelectedElement is not null);

    [field: AllowNull, MaybeNull]
    public ICommand CloseGridCommand => field
        ??= new LambdaCommand(() =>
        {
            IsGridOpen   = false;
            SelectedSpot = null;
        });

    [field: AllowNull, MaybeNull]
    public ICommand RebuildGridCommand => field
        ??= new LambdaCommandAsync(RebuildGridAsync,
            () => EditingGridElement?.CanModifyGrid == true);

    private async Task RebuildGridAsync()
    {
        if (EditingGridElement is not { } editing) return;
        var result = await gardenService.ResizeGridAsync(new(editing.Id, editing.GridRows, editing.GridColumns));
        result.Match(saved =>
        {
            editing.GridRows = saved.Rows;
            editing.GridColumns = saved.Columns;
            editing.PlantingSpots = new ObservableCollection<PlantingSpotFromViewModel>(saved.Spots.Select(GardenPlanMapper.MapSpot));
            if (ReferenceEquals(editing, EditingGridElement)) SelectedSpot = null;
            return true;
        }, error =>
        {
            userDialog.Warning(error, "Сетка посадок");
            return false;
        });
    }

    // --- Выделение ячейки ---

    [field: AllowNull, MaybeNull]
    public ICommand SelectSpotCommand => field
        ??= new LambdaCommand(p =>
        {
            // Повторный клик по выбранной ячейке снимает выделение
            if (p is PlantingSpotFromViewModel vm && vm != SelectedSpot)
                SelectedSpot = vm;
            else
                SelectedSpot = null;
        });

    // --- Переходы состояния ячейки ---

    [field: AllowNull, MaybeNull]
    public ICommand ReserveSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await PlanSpotReserveAsync(p as PlantingSpotFromViewModel),
            _ => true);

    private async Task PlanSpotReserveAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null) return;
        var label = string.IsNullOrWhiteSpace(PlanPickerLabel) ? null : PlanPickerLabel.Trim();
        await ApplySpotChangeAsync(spotVm, new SpotChange.Transition(new ReservedSpotState(), label.ToOption()));
    }

    [field: AllowNull, MaybeNull]
    public ICommand PlantSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await PlantSpotAsync(p as PlantingSpotFromViewModel),
            _ => SelectedPlantSource is not null);

    private async Task PlantSpotAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null || SelectedPlantSource is null) return;
        var source = SelectedPlantSource;

        // Полный адрес посадки: план / участок / теплица (если есть) / элемент
        var plantPlace = string.Join(" / ", new[]
        {
            SelectedPlan?.Name,
            SelectedGarden?.Name,
            SelectedGreenhouse?.Name,
            EditingGridElement?.Name
        }.Where(s => !string.IsNullOrEmpty(s)));

        var result = await plantingService.PlantAsync(new PlantRequest(
            SpotId:        spotVm.Id,
            Kind:          source.Kind,
            EntityId:      source.EntityId,
            IsWeightBased: source.IsWeightBased,
            Amount:        PlantingAmount,
            PlantedDate:   NewPlantingDate,
            PlantName:     source.PlantName,
            PlantPlace:    string.IsNullOrEmpty(plantPlace) ? null : plantPlace));

        result.Match(receipt =>
        {
            // Порт возвращает свежий остаток с учётом фактического расхода.
            source.AvailableQty = receipt.Remaining.Value;
            if (source.AvailableQty <= 0)
            {
                if (source.Kind == PlantSourceKind.Seedling)
                    AvailableSeedlingItems.Remove(source);
                else
                    AvailableSeedItems.Remove(source);
            }

            spotVm.SeedlingInfoId = receipt.SeedlingInfoId;
            UpdateSpotVm(spotVm, new PlantedSpotState(), NewPlantingDate, source.PlantName);
            SelectedPlantSource = null;
            return true;
        }, error =>
        {
            userDialog.Warning(error, "Посадка");
            return false;
        });
    }

    [field: AllowNull, MaybeNull]
    public ICommand HarvestSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeSpotAsync(p as PlantingSpotFromViewModel, new HarvestedSpotState()),
            _ => true);

    [field: AllowNull, MaybeNull]
    public ICommand MarkDeadSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeSpotAsync(p as PlantingSpotFromViewModel, new DeadSpotState()),
            _ => true);

    [field: AllowNull, MaybeNull]
    public ICommand ClearSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ClearSpotAsync(p as PlantingSpotFromViewModel),
            _ => true);

    private async Task ChangeSpotAsync(PlantingSpotFromViewModel? spotVm, PlantingSpotState newState)
    {
        if (spotVm is null) return;
        await ApplySpotChangeAsync(spotVm, new SpotChange.Transition(newState, Option.None<string>()));
    }

    private async Task ClearSpotAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null) return;
        await ApplySpotChangeAsync(spotVm, new SpotChange.Clear());
    }

    private async Task ApplySpotChangeAsync(PlantingSpotFromViewModel spot, SpotChange change)
    {
        var result = await gardenService.ChangeStateAsync(new(spot.Id, new StateChangeTarget.Spot(change)));
        ApplyStateResult(result, saved =>
        {
            var cell = saved switch
            {
                GardenStateResultFromViewModel.Spot changed => changed.Cell,
                _ => throw new System.Diagnostics.UnreachableException()
            };
            spot.StateTypeName = cell.StateTypeName;
            spot.SeedlingInfoId = cell.SeedlingInfoId.ToNullable();
            spot.PlantedDate = cell.PlantedDate.ToNullable();
            spot.Note = cell.Note.ToNullable();
            spot.PlantLabel = change is SpotChange.Clear ? null : cell.Note.ToNullable();
        });
    }

    private void ApplyStateResult(Result<GardenStateResultFromViewModel> result, Action<GardenStateResultFromViewModel> success)
        => result.Match(saved =>
        {
            success(saved);
            return true;
        }, error =>
        {
            userDialog.Warning(error, "Состояние планировщика");
            return false;
        });

    // ─────────────────────────────────────
    //  Вспомогательные методы маппинга
    // ─────────────────────────────────────

    /// <summary>
    /// Обновляет VM ячейки после изменения состояния.
    /// StateTypeName автоматически пересчитывает CellColor, CanGoTo* и прочее через INPC.
    /// </summary>
    private static void UpdateSpotVm(PlantingSpotFromViewModel vm,
        PlantingSpotState state, DateTime? plantedDate = null, string? plantLabel = null)
    {
        vm.StateTypeName = state.GetType().Name;
        if (plantedDate.HasValue)  vm.PlantedDate = plantedDate;
        if (plantLabel is not null) vm.PlantLabel  = plantLabel;
    }

}
