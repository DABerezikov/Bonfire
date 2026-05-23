using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using BonfireDB.Entities.GardenPlanning.States;

namespace Bonfire.ViewModels;

public class GardenPlanViewModel : ViewModel
{
    private readonly IGardenService _gardenService;
    private readonly IUserDialog _userDialog;
    private readonly ISeedlingsService _seedlingsService;
    private readonly ISeedsService _seedsService;
    private readonly IPlantingService _plantingService;

    public GardenPlanViewModel(IGardenService gardenService, IUserDialog userDialog,
        ISeedlingsService seedlingsService, ISeedsService seedsService,
        IPlantingService plantingService)
    {
        _gardenService    = gardenService;
        _userDialog       = userDialog;
        _seedlingsService = seedlingsService;
        _seedsService     = seedsService;
        _plantingService  = plantingService;
    }

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

    public ObservableCollection<GardenPlan> Plans
    {
        get;
        set => Set(ref field, value);
    } = [];

    public GardenPlan? SelectedPlan
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
            if (Set(ref field, value))
            {
                SelectedElement = null;
                OnPropertyChanged(nameof(GardenPropertiesPanel));
                if (value is not null) RecalculateInitialZoom();
            }
        }
    }

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

    public ICommand ZoomInCommand    => field ??= new LambdaCommand(() => GardenZoom += 0.1);
    public ICommand ZoomOutCommand   => field ??= new LambdaCommand(() => GardenZoom -= 0.1);
    public ICommand ResetZoomCommand => field ??= new LambdaCommand(RecalculateInitialZoom);

    private void UpdateElementZoom()
    {
        if (SelectedGarden is null) return;
        foreach (var el in SelectedGarden.Elements)
            el.CanvasZoom = _gardenZoom;
        foreach (var gh in SelectedGarden.Greenhouses)
            foreach (var el in gh.InnerElements)
                el.CanvasZoom = _gardenZoom;
    }

    public void RecalculateInitialZoom()
    {
        if (SelectedGarden is null) { GardenZoom = 1.0; return; }
        const double targetPx = 80.0;
        var sizes = SelectedGarden.Elements
            .Select(e => Math.Min(e.Width, e.Height))
            .Concat(SelectedGarden.Greenhouses.Select(gh => Math.Min(gh.DisplayWidth, gh.DisplayHeight)));
        double smallest = sizes.DefaultIfEmpty(targetPx).Min();
        GardenZoom = Math.Min(smallest > 0 ? targetPx / smallest : 1.0, 4.0);
    }

    // --- Выбранный элемент на холсте ---

    public GardenElementFromViewModel? SelectedElement
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                OnPropertyChanged(nameof(GardenPropertiesPanel));
                OnPropertyChanged(nameof(GreenhousePropertiesPanel));
            }
        }
    }

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
            if (Set(ref field, value))
            {
                SelectedElement = null;
                OnPropertyChanged(nameof(GreenhousePropertiesPanel));
                OnPropertyChanged(nameof(IsEditingGreenhouse));
            }
        }
    }

    /// <summary>True, когда открыт оверлей теплицы.</summary>
    public bool IsEditingGreenhouse => SelectedGreenhouse is not null;

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

    private System.Collections.ObjectModel.ObservableCollection<GardenElementFromViewModel> ActiveElements =>
        SelectedGreenhouse?.InnerElements ?? SelectedGarden?.Elements
        ?? new System.Collections.ObjectModel.ObservableCollection<GardenElementFromViewModel>();

    private double ActiveCanvasWidth =>
        SelectedGreenhouse?.InnerCanvasWidth ?? SelectedGarden?.CanvasWidth ?? 0;

    private double ActiveCanvasHeight =>
        SelectedGreenhouse?.InnerCanvasHeight ?? SelectedGarden?.CanvasHeight ?? 0;

    private System.Collections.ObjectModel.ObservableCollection<GreenhouseFromViewModel> ActiveGreenhouses =>
        SelectedGreenhouse is null
            ? (SelectedGarden?.Greenhouses
               ?? new System.Collections.ObjectModel.ObservableCollection<GreenhouseFromViewModel>())
            : new System.Collections.ObjectModel.ObservableCollection<GreenhouseFromViewModel>();

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
            if (Set(ref field, value))
            {
                PlantingAmount = 1;
                OnPropertyChanged(nameof(PlantingAmountUnit));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
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

    public ICommand LoadDataCommand => field
        ??= new LambdaCommandAsync(LoadDataAsync);

    private async Task LoadDataAsync()
    {
        var planList = await _gardenService.GetPlansOrderedByYearDescAsync();

        Plans = new ObservableCollection<GardenPlan>(planList);
        SelectedPlan = Plans.FirstOrDefault();

        await LoadPlantLabelsAsync();
    }

    private async Task LoadPlantLabelsAsync()
    {
        // ── Рассада: из рассады сажают ВЗОШЕДШИЕ живые ростки штучно. ───────
        // Доступно = взошедшие живые − уже высаженные (вес рассады не при чём).
        var allSeedlings = await _seedlingsService.GetAllSeedlingsAsync();

        AvailableSeedlingItems = new ObservableCollection<PlantSourceItem>(
            allSeedlings
            .Where(s => !s.IsPlantedInBed) // уже высаженные в грядку партии повторно не сажаем
            .Select(s =>
            {
                var name = $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}";
                var qty  = Bonfire.Services.SeedlingAvailability.Available(s);
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
        var seedEntities = (await _seedsService.GetAllSeedsAsync())
            .Where(s => s.SeedsInfo.AmountSeeds > 0 || s.SeedsInfo.AmountSeedsWeight > 0)
            .ToList();

        AvailableSeedItems = new ObservableCollection<PlantSourceItem>(
            seedEntities
            .Select(s =>
            {
                var name        = $"{s.Plant.PlantCulture.Name} {s.Plant.PlantSort.Name}";
                var weightBased = s.SeedsInfo.AmountSeeds <= 0 && s.SeedsInfo.AmountSeedsWeight > 0;
                var qty         = weightBased ? (s.SeedsInfo.AmountSeedsWeight ?? 0) : s.SeedsInfo.AmountSeeds;
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
        if (SelectedPlan is null) { Gardens.Clear(); return; }

        var gardens = await _gardenService.GetGardensByPlanAsync(SelectedPlan.Id);

        Gardens = new ObservableCollection<GardenFromViewModel>(
            gardens.Select(GardenPlanMapper.MapGarden));
        SelectedGarden = Gardens.FirstOrDefault();
    }

    // ─────────────────────────────────────
    //  Управление планами
    // ─────────────────────────────────────

    public ICommand CreatePlanCommand => field
        ??= new LambdaCommandAsync(CreatePlanAsync);

    private async Task CreatePlanAsync()
    {
        var year = DateTime.Now.Year;
        var name = $"План {year}";
        var plan = await _gardenService.CreatePlanAsync(name, year);
        Plans.Insert(0, plan);
        SelectedPlan = plan;
    }

    public ICommand DeletePlanCommand => field
        ??= new LambdaCommandAsync(DeletePlanAsync,
            () => SelectedPlan is not null);

    private async Task DeletePlanAsync()
    {
        if (SelectedPlan is null) return;
        if (!_userDialog.YesNoQuestion(
                $"Удалить план «{SelectedPlan.Name}»? Все участки и элементы будут удалены.",
                "Удаление плана")) return;

        await _gardenService.DeletePlanAsync(SelectedPlan);
        Plans.Remove(SelectedPlan);
        SelectedPlan = Plans.FirstOrDefault();
    }

    // ─────────────────────────────────────
    //  Управление участком
    // ─────────────────────────────────────

    public ICommand CreateGardenCommand => field
        ??= new LambdaCommandAsync(CreateGardenAsync,
            () => SelectedPlan is not null);

    private async Task CreateGardenAsync()
    {
        if (SelectedPlan is null) return;
        var garden = await _gardenService.CreateGardenAsync(
            SelectedPlan.Id, "Новый участок", 20, 15, scale: 40);
        var vm = GardenPlanMapper.MapGarden(garden);
        Gardens.Add(vm);
        SelectedGarden = vm;
    }

    public ICommand DeleteGardenCommand => field
        ??= new LambdaCommandAsync(DeleteGardenAsync,
            () => SelectedGarden is not null);

    private async Task DeleteGardenAsync()
    {
        if (SelectedGarden is null) return;
        if (!_userDialog.YesNoQuestion(
                $"Удалить участок «{SelectedGarden.Name}»? Все элементы и посадки будут удалены.",
                "Удаление участка")) return;

        var entity = await _gardenService.GetGardenByIdAsync(SelectedGarden.Id);
        if (entity is null) return;

        await _gardenService.DeleteGardenAsync(entity);
        Gardens.Remove(SelectedGarden);
        SelectedGarden = Gardens.FirstOrDefault();
    }

    public ICommand SaveGardenSizeCommand => field
        ??= new LambdaCommandAsync(SaveGardenSizeAsync,
            () => SelectedGarden is not null);

    private async Task SaveGardenSizeAsync()
    {
        if (SelectedGarden is null) return;

        const double scale = 40;
        double newCanvasW = SelectedGarden.WidthMeters * scale;
        double newCanvasH = SelectedGarden.HeightMeters * scale;

        // Проверяем, что ни один элемент не выходит за новые границы
        var blockedElement = SelectedGarden.Elements
            .FirstOrDefault(el => el.X + el.Width > newCanvasW || el.Y + el.Height > newCanvasH);
        if (blockedElement is not null)
        {
            _userDialog.Warning(
                $"Нельзя уменьшить участок: элемент «{blockedElement.Name}» " +
                $"(правый/нижний край {blockedElement.X + blockedElement.Width:F0}×{blockedElement.Y + blockedElement.Height:F0} пкс) " +
                $"выходит за новые границы {newCanvasW:F0}×{newCanvasH:F0} пкс.",
                "Изменение размера участка");
            return;
        }

        var blockedGh = SelectedGarden.Greenhouses
            .FirstOrDefault(gh => gh.X + gh.DisplayWidth > newCanvasW || gh.Y + gh.DisplayHeight > newCanvasH);
        if (blockedGh is not null)
        {
            _userDialog.Warning(
                $"Нельзя уменьшить участок: теплица «{blockedGh.Name}» выходит за новые границы.",
                "Изменение размера участка");
            return;
        }

        var entity = await _gardenService.GetGardenByIdAsync(SelectedGarden.Id);
        if (entity is null) return;

        entity.Name          = SelectedGarden.Name;
        entity.WidthMeters   = SelectedGarden.WidthMeters;
        entity.HeightMeters  = SelectedGarden.HeightMeters;
        entity.CanvasWidth   = newCanvasW;
        entity.CanvasHeight  = newCanvasH;
        entity.Address       = SelectedGarden.Address;
        entity.Note          = SelectedGarden.Note;

        await _gardenService.UpdateGardenAsync(entity);

        SelectedGarden.CanvasWidth  = newCanvasW;
        SelectedGarden.CanvasHeight = newCanvasH;

        // Обновляем контейнерные размеры у всех элементов на участке
        foreach (var el in SelectedGarden.Elements)
        {
            el.ContainerCanvasWidth  = newCanvasW;
            el.ContainerCanvasHeight = newCanvasH;
        }
        foreach (var gh in SelectedGarden.Greenhouses)
        {
            gh.ContainerCanvasWidth  = newCanvasW;
            gh.ContainerCanvasHeight = newCanvasH;
        }
    }

    // ─────────────────────────────────────
    //  Выделение и навигация
    // ─────────────────────────────────────

    public ICommand SelectElementCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is GardenElementFromViewModel vm)
            {
                if (SelectedElement is not null)
                    SelectedElement.IsSelected = false;
                SelectedElement = vm;
                vm.IsSelected = true;
            }
        });

    /// <summary>
    /// Снимает выделение с элемента. Вызывается при клике по пустому месту холста или нажатии Escape.
    /// </summary>
    public ICommand DeselectElementCommand => field
        ??= new LambdaCommand(() =>
        {
            if (SelectedElement is not null)
                SelectedElement.IsSelected = false;
            SelectedElement = null;
        });

    public ICommand SetElementTypeCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is string type)
                SelectedElementType = type;
        });

    public ICommand OpenGreenhouseCommand => field
        ??= new LambdaCommand(p =>
        {
            if (p is GreenhouseFromViewModel gh)
                SelectedGreenhouse = gh;
        });

    public ICommand CloseGreenhouseCommand => field
        ??= new LambdaCommand(() => SelectedGreenhouse = null);

    public ICommand ChangeGreenhouseStateCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeGreenhouseStateAsync(p as string),
            _ => SelectedGreenhouse is not null);

    private async Task ChangeGreenhouseStateAsync(string? stateTypeName)
    {
        if (SelectedGreenhouse is null || stateTypeName is null) return;

        var newState = GardenElementState.From(stateTypeName);
        var currentState = GardenElementState.From(SelectedGreenhouse.StateTypeName);
        if (!currentState.CanTransitionTo(newState)) return;

        var entity = await _gardenService.GetGreenhouseByIdAsync(SelectedGreenhouse.Id);
        if (entity is null) return;

        await _gardenService.ChangeGreenhouseStateAsync(entity, newState);

        SelectedGreenhouse.StateTypeName    = newState.GetType().Name;
        SelectedGreenhouse.StateDisplayName = newState.DisplayName;
        SelectedGreenhouse.StateColor       = newState.StatusColor;
    }

    public ICommand DeleteGreenhouseCommand => field
        ??= new LambdaCommandAsync(DeleteGreenhouseAsync,
            () => SelectedGreenhouse is not null);

    private async Task DeleteGreenhouseAsync()
    {
        if (SelectedGreenhouse is null || SelectedGarden is null) return;
        if (!_userDialog.YesNoQuestion(
                $"Удалить теплицу «{SelectedGreenhouse.Name}»? Все элементы и посадки будут удалены.",
                "Удаление теплицы")) return;

        var entity = await _gardenService.GetGreenhouseByIdAsync(SelectedGreenhouse.Id);
        if (entity is null) return;

        await _gardenService.DeleteGreenhouseAsync(entity);
        SelectedGarden.Greenhouses.Remove(SelectedGreenhouse);
        SelectedGreenhouse = null;
    }

    public ICommand SaveGreenhouseSizeCommand => field
        ??= new LambdaCommandAsync(SaveGreenhouseSizeAsync,
            () => SelectedGreenhouse is not null);

    private async Task SaveGreenhouseSizeAsync()
    {
        if (SelectedGreenhouse is null) return;

        const double scale = 40;
        double newInnerW = SelectedGreenhouse.WidthMeters * scale;
        double newInnerH = SelectedGreenhouse.HeightMeters * scale;

        var blockedElement = SelectedGreenhouse.InnerElements
            .FirstOrDefault(el => el.X + el.Width > newInnerW || el.Y + el.Height > newInnerH);
        if (blockedElement is not null)
        {
            _userDialog.Warning(
                $"Нельзя уменьшить теплицу: элемент «{blockedElement.Name}» " +
                $"(правый/нижний край {blockedElement.X + blockedElement.Width:F0}×{blockedElement.Y + blockedElement.Height:F0} пкс) " +
                $"выходит за новые границы {newInnerW:F0}×{newInnerH:F0} пкс.",
                "Изменение размера теплицы");
            return;
        }

        var entity = await _gardenService.GetGreenhouseByIdAsync(SelectedGreenhouse.Id);
        if (entity is null) return;

        entity.Name         = SelectedGreenhouse.Name;
        entity.WidthMeters  = SelectedGreenhouse.WidthMeters;
        entity.HeightMeters = SelectedGreenhouse.HeightMeters;
        entity.CanvasWidth  = newInnerW;
        entity.CanvasHeight = newInnerH;
        entity.Note         = SelectedGreenhouse.Note;
        entity.Material     = SelectedGreenhouse.Material;

        await _gardenService.UpdateGreenhouseAsync(entity);

        SelectedGreenhouse.InnerCanvasWidth  = newInnerW;
        SelectedGreenhouse.InnerCanvasHeight = newInnerH;

        foreach (var el in SelectedGreenhouse.InnerElements)
        {
            el.ContainerCanvasWidth  = newInnerW;
            el.ContainerCanvasHeight = newInnerH;
        }
    }

    // ─────────────────────────────────────
    //  Добавление элементов
    // ─────────────────────────────────────

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

        // Размеры по умолчанию в пикселях (масштаб 40 пкс/м), Д=Width(горизонталь), Ш=Height(вертикаль):
        //   Грядка        Д 2.0 × Ш 0.9 м = 80 × 36 пкс
        //   Парник        Д 1.2 × Ш 0.8 м = 48 × 32 пкс
        //   Цветник       Д 0.6 × Ш 0.6 м = 24 × 24 пкс
        //   Открытый грунт Д 2.0 × Ш 2.0 м = 80 × 80 пкс
        (double defaultW, double defaultH) = SelectedElementType switch
        {
            "FlowerBed"      => (24.0, 24.0),
            "ColdFrame"      => (48.0, 32.0),
            "OpenGroundArea" => (80.0, 80.0),
            _                => (80.0, 36.0)   // Bed
        };

        var spot = CollisionHelper.FindFreeSpot(
            activeElements, activeGreenhouses, exclude: null,
            defaultW, defaultH, canvasW, canvasH);

        if (spot is null)
        {
            var containerName = SelectedGreenhouse?.Name ?? SelectedGarden.Name;
            _userDialog.Warning(
                $"В «{containerName}» нет свободного места для нового элемента.\n" +
                "Увеличьте размер или уберите лишние элементы.",
                "Добавление элемента");
            return;
        }

        GardenElement element = SelectedElementType switch
        {
            "ColdFrame"      => new ColdFrame(),
            "FlowerBed"      => new FlowerBed(),
            "OpenGroundArea" => new OpenGroundArea(),
            _                => new Bed()
        };

        element.PlotId        = ActiveContainerId;
        element.Name          = GetDefaultName(SelectedElementType, activeElements.Count + 1);
        element.X             = spot.Value.x;
        element.Y             = spot.Value.y;
        element.DisplayWidth  = defaultW;
        element.DisplayHeight = defaultH;

        var saved = await _gardenService.AddElementAsync(element);
        var vm = GardenPlanMapper.MapElement(saved, canvasW, canvasH);
        vm.ContainerElements    = activeElements;
        vm.ContainerGreenhouses = activeGreenhouses;
        activeElements.Add(vm);
        SelectedElement = vm;
    }

    public ICommand AddGreenhouseCommand => field
        ??= new LambdaCommandAsync(AddGreenhouseAsync,
            () => SelectedGarden is not null);

    private async Task AddGreenhouseAsync()
    {
        if (SelectedGarden is null) return;

        const double scale = 40;
        const double ghW = 6 * scale, ghH = 3 * scale; // 6×3 м по умолчанию

        var spot = CollisionHelper.FindFreeSpot(
            SelectedGarden.Elements, SelectedGarden.Greenhouses, exclude: null,
            ghW, ghH, SelectedGarden.CanvasWidth, SelectedGarden.CanvasHeight);

        if (spot is null)
        {
            _userDialog.Warning(
                $"На участке «{SelectedGarden.Name}» нет свободного места для теплицы 6×3 м.\n" +
                "Увеличьте участок или уберите лишние элементы.",
                "Добавление теплицы");
            return;
        }

        var gh = await _gardenService.AddGreenhouseAsync(
            SelectedGarden.Id,
            $"Теплица {SelectedGarden.Greenhouses.Count + 1}",
            widthMeters: 6, heightMeters: 3, scale: scale,
            x: spot.Value.x, y: spot.Value.y);
        var vm = GardenPlanMapper.MapGreenhouse(gh, SelectedGarden.CanvasWidth, SelectedGarden.CanvasHeight);
        vm.ContainerElements    = SelectedGarden.Elements;
        vm.ContainerGreenhouses = SelectedGarden.Greenhouses;
        SelectedGarden.Greenhouses.Add(vm);
    }

    // ─────────────────────────────────────
    //  Удаление / перемещение элементов
    // ─────────────────────────────────────

    public ICommand DeleteElementCommand => field
        ??= new LambdaCommandAsync(DeleteElementAsync,
            () => SelectedElement is not null);

    private async Task DeleteElementAsync()
    {
        if (SelectedElement is null || SelectedGarden is null) return;
        var entity = await _gardenService.GetElementByIdAsync(SelectedElement.Id);
        if (entity is null) return;

        await _gardenService.DeleteElementAsync(entity);
        ActiveElements.Remove(SelectedElement);
        SelectedElement = null;
    }

    public ICommand ToggleLockElementCommand => field
        ??= new LambdaCommandAsync(
            async p => await ToggleLockElementAsync(p as GardenElementFromViewModel),
            _ => true);

    private async Task ToggleLockElementAsync(GardenElementFromViewModel? vm)
    {
        if (vm is null) return;
        vm.IsLocked = !vm.IsLocked;
        var entity = await _gardenService.GetElementByIdAsync(vm.Id);
        if (entity is null) return;
        entity.IsLocked = vm.IsLocked;
        await _gardenService.UpdateElementAsync(entity);
    }

    public ICommand ToggleLockGreenhouseCommand => field
        ??= new LambdaCommandAsync(
            async p => await ToggleLockGreenhouseAsync(p as GreenhouseFromViewModel),
            _ => true);

    private async Task ToggleLockGreenhouseAsync(GreenhouseFromViewModel? vm)
    {
        if (vm is null) return;
        vm.IsLocked = !vm.IsLocked;
        var entity = await _gardenService.GetGreenhouseByIdAsync(vm.Id);
        if (entity is null) return;
        entity.IsLocked = vm.IsLocked;
        await _gardenService.UpdateGreenhouseAsync(entity);
    }

    public ICommand SaveElementPositionCommand => field
        ??= new LambdaCommandAsync(SaveElementPositionAsync,
            () => SelectedElement is not null);

    private async Task SaveElementPositionAsync()
    {
        if (SelectedElement is null) return;
        var entity = await _gardenService.GetElementByIdAsync(SelectedElement.Id);
        if (entity is null) return;

        entity.X = SelectedElement.X;
        entity.Y = SelectedElement.Y;
        entity.DisplayWidth = SelectedElement.Width;
        entity.DisplayHeight = SelectedElement.Height;
        entity.AreaSquareMeters = SelectedElement.AreaSquareMeters;
        entity.Name = SelectedElement.Name;
        entity.Note = SelectedElement.Note;
        entity.SoilType = SelectedElement.SoilType;
        if (entity is Bed bed)
            bed.Orientation = SelectedElement.Orientation;
        if (entity is ColdFrame cf)
            cf.CoverMaterial = SelectedElement.CoverMaterial;

        await _gardenService.UpdateElementAsync(entity);
    }

    public ICommand SaveGreenhousePositionCommand => field
        ??= new LambdaCommandAsync(
            async p => await SaveGreenhousePositionAsync(p as GreenhouseFromViewModel),
            _ => true);

    private async Task SaveGreenhousePositionAsync(GreenhouseFromViewModel? vm)
    {
        if (vm is null) return;
        var entity = await _gardenService.GetGreenhouseByIdAsync(vm.Id);
        if (entity is null) return;

        entity.X = vm.X;
        entity.Y = vm.Y;
        entity.DisplayWidth = vm.DisplayWidth;
        entity.DisplayHeight = vm.DisplayHeight;
        entity.Name = vm.Name;
        entity.Note = vm.Note;

        await _gardenService.UpdateGreenhouseAsync(entity);
    }

    // ─────────────────────────────────────
    //  Переходы состояний
    // ─────────────────────────────────────

    public ICommand ChangeElementStateCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeElementStateAsync(p as string),
            _ => SelectedElement is not null);

    private async Task ChangeElementStateAsync(string? stateTypeName)
    {
        if (SelectedElement is null || stateTypeName is null) return;

        // Проверяем допустимость перехода до обращения к БД — кнопка должна быть задизаблена,
        // но на случай гонки или вызова из ContextMenu дублируем проверку здесь.
        var newState = GardenElementState.From(stateTypeName);
        var currentState = GardenElementState.From(SelectedElement.StateTypeName);
        if (!currentState.CanTransitionTo(newState)) return;

        var entity = await _gardenService.GetElementByIdAsync(SelectedElement.Id);
        if (entity is null) return;

        await _gardenService.ChangeElementStateAsync(entity, newState);

        SelectedElement.StateTypeName = newState.GetType().Name;
        SelectedElement.StateDisplayName = newState.DisplayName;
        SelectedElement.StateColor = newState.StatusColor;
        SelectedElement.CanAddPlanting = newState.CanAddPlanting;
        SelectedElement.CanModifyGrid = newState.CanModifyGrid;
    }

    // ─────────────────────────────────────
    //  Сетка посадок
    // ─────────────────────────────────────

    public ICommand OpenPlantingGridCommand => field
        ??= new LambdaCommand(() =>
        {
            EditingGridElement = SelectedElement;
            SelectedSpot       = null;
            IsGridOpen         = SelectedElement is not null;
        }, () => SelectedElement is not null);

    public ICommand CloseGridCommand => field
        ??= new LambdaCommand(() =>
        {
            IsGridOpen   = false;
            SelectedSpot = null;
        });

    public ICommand RebuildGridCommand => field
        ??= new LambdaCommandAsync(RebuildGridAsync,
            () => EditingGridElement?.CanModifyGrid == true);

    private async Task RebuildGridAsync()
    {
        if (EditingGridElement is null) return;
        var entity = await _gardenService.GetElementByIdAsync(EditingGridElement.Id);
        if (entity is null) return;

        await _gardenService.RebuildGridAsync(entity, EditingGridElement.GridRows, EditingGridElement.GridColumns);

        SelectedSpot = null;
        RebuildSpotsVm(EditingGridElement, entity);
    }

    // --- Выделение ячейки ---

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

    public ICommand ReserveSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await PlanSpotReserveAsync(p as PlantingSpotFromViewModel),
            _ => true);

    private async Task PlanSpotReserveAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null) return;
        var entity = await LoadSpotEntityAsync(spotVm.Id);
        if (entity is null) return;
        var newState = new ReservedSpotState();
        if (!entity.State.CanTransitionTo(newState)) return;

        // Сохраняем метку растения в Note — инвентарь НЕ расходуется
        var label = string.IsNullOrWhiteSpace(PlanPickerLabel) ? null : PlanPickerLabel.Trim();
        await _gardenService.ChangeSpotStateAsync(entity, newState, plantLabel: label);
        UpdateSpotVm(spotVm, newState, plantLabel: label);
    }

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

        var result = await _plantingService.PlantAsync(new PlantRequest(
            SpotId:        spotVm.Id,
            Kind:          source.Kind,
            EntityId:      source.EntityId,
            IsWeightBased: source.IsWeightBased,
            Amount:        PlantingAmount,
            PlantedDate:   NewPlantingDate,
            PlantName:     source.PlantName,
            PlantPlace:    string.IsNullOrEmpty(plantPlace) ? null : plantPlace));

        if (!result.Success) return;

        // Списываем остаток в пикере и убираем позицию, если исчерпана
        source.AvailableQty -= PlantingAmount;
        if (source.AvailableQty <= 0)
        {
            if (source.Kind == PlantSourceKind.Seedling)
                AvailableSeedlingItems.Remove(source);
            else
                AvailableSeedItems.Remove(source);
        }

        spotVm.SeedlingInfoId = result.SeedlingInfoId;
        UpdateSpotVm(spotVm, new PlantedSpotState(), NewPlantingDate, source.PlantName);

        SelectedPlantSource = null;   // сброс пикера и количества после посадки
    }

    public ICommand HarvestSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeSpotAsync(p as PlantingSpotFromViewModel, new HarvestedSpotState()),
            _ => true);

    public ICommand MarkDeadSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ChangeSpotAsync(p as PlantingSpotFromViewModel, new DeadSpotState()),
            _ => true);

    public ICommand ClearSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await ClearSpotAsync(p as PlantingSpotFromViewModel),
            _ => true);

    private async Task ChangeSpotAsync(PlantingSpotFromViewModel? spotVm, PlantingSpotState newState)
    {
        if (spotVm is null) return;
        var entity = await LoadSpotEntityAsync(spotVm.Id);
        if (entity is null) return;
        if (!entity.State.CanTransitionTo(newState)) return;

        await _gardenService.ChangeSpotStateAsync(entity, newState);
        UpdateSpotVm(spotVm, newState);
    }

    private async Task ClearSpotAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null) return;
        var entity = await LoadSpotEntityAsync(spotVm.Id);
        if (entity is null) return;

        await _gardenService.ClearSpotAsync(entity);

        var emptyState = new EmptySpotState();
        spotVm.StateTypeName = emptyState.GetType().Name;
        spotVm.PlantedDate   = null;
        spotVm.PlantLabel    = null;
    }

    private async Task<PlantingSpot?> LoadSpotEntityAsync(int spotId)
        => await _gardenService.GetSpotAsync(spotId);

    // ─────────────────────────────────────
    //  Вспомогательные методы маппинга
    // ─────────────────────────────────────

    private static void RebuildSpotsVm(GardenElementFromViewModel vm, GardenElement entity)
    {
        vm.GridRows = entity.GridRows;
        vm.GridColumns = entity.GridColumns;
        vm.PlantingSpots = new ObservableCollection<PlantingSpotFromViewModel>(
            entity.PlantingSpots.OrderBy(s => s.Row).ThenBy(s => s.Column).Select(GardenPlanMapper.MapSpot));
    }

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

    private static string GetDefaultName(string type, int index) => type switch
    {
        "Bed"            => $"Грядка {index}",
        "ColdFrame"      => $"Парник {index}",
        "FlowerBed"      => $"Цветник {index}",
        "OpenGroundArea" => $"Открытый грунт {index}",
        _                => $"Элемент {index}"
    };
}
