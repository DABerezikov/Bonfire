using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using BonfireDB.Entities.GardenPlanning.States;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.ViewModels;

public class GardenPlanViewModel : ViewModel
{
    private readonly IGardenService _gardenService;
    private readonly IUserDialog _userDialog;

    public GardenPlanViewModel(IGardenService gardenService, IUserDialog userDialog)
    {
        _gardenService = gardenService;
        _userDialog = userDialog;
    }

    public bool IsActive { get; set; }

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
                LoadGardens();
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
            }
        }
    }

    // --- Выбранный элемент на холсте ---

    public GardenElementFromViewModel? SelectedElement
    {
        get;
        set
        {
            if (Set(ref field, value))
                OnPropertyChanged(nameof(GardenPropertiesPanel));
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
        set => Set(ref field, value);
    }

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

    /// <summary>Название того, что сажаем (сохраняется в PlantingSpot.Note).</summary>
    public string NewPlantingLabel
    {
        get;
        set => Set(ref field, value);
    } = "";

    /// <summary>Дата посадки — по умолчанию сегодня.</summary>
    public DateTime NewPlantingDate
    {
        get;
        set => Set(ref field, value);
    } = DateTime.Today;

    // ─────────────────────────────────────
    //  Загрузка данных
    // ─────────────────────────────────────

    public ICommand LoadDataCommand => field
        ??= new LambdaCommandAsync(LoadDataAsync);

    private async Task LoadDataAsync()
    {
        var planList = await _gardenService.Plans
            .OrderByDescending(p => p.Year)
            .ToListAsync();

        Plans = new ObservableCollection<GardenPlan>(planList);
        SelectedPlan = Plans.FirstOrDefault();
    }

    private void LoadGardens()
    {
        if (SelectedPlan is null) { Gardens.Clear(); return; }

        var gardenVms = _gardenService.Gardens
            .Where(g => g.GardenPlanId == SelectedPlan.Id)
            .AsEnumerable()
            .Select(MapGarden)
            .ToList();

        Gardens = new ObservableCollection<GardenFromViewModel>(gardenVms);
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
        var vm = MapGarden(garden);
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

        var entity = await _gardenService.Gardens
            .FirstOrDefaultAsync(g => g.Id == SelectedGarden.Id);
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

        var entity = await _gardenService.Gardens
            .FirstOrDefaultAsync(g => g.Id == SelectedGarden.Id);
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

    // ─────────────────────────────────────
    //  Добавление элементов
    // ─────────────────────────────────────

    public ICommand AddElementCommand => field
        ??= new LambdaCommandAsync(AddElementAsync,
            () => SelectedGarden is not null);

    private async Task AddElementAsync()
    {
        if (SelectedGarden is null) return;

        // Размеры нового элемента по умолчанию (совпадают с GardenElement.DisplayWidth/Height)
        const double defaultW = 120, defaultH = 80;

        // Ищем первое свободное место с учётом элементов и теплиц
        var spot = CollisionHelper.FindFreeSpot(
            SelectedGarden.Elements, SelectedGarden.Greenhouses, exclude: null,
            defaultW, defaultH,
            SelectedGarden.CanvasWidth, SelectedGarden.CanvasHeight);

        if (spot is null)
        {
            _userDialog.Warning(
                $"На участке «{SelectedGarden.Name}» нет свободного места для нового элемента.\n" +
                "Увеличьте размер участка или уберите лишние элементы.",
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

        element.PlotId = SelectedGarden.Id;
        element.Name = GetDefaultName(SelectedElementType, SelectedGarden.Elements.Count + 1);
        element.X = spot.Value.x;
        element.Y = spot.Value.y;
        // DisplayWidth/DisplayHeight уже 120/80 по умолчанию в GardenElement

        var saved = await _gardenService.AddElementAsync(element);
        var vm = MapElement(saved, SelectedGarden.CanvasWidth, SelectedGarden.CanvasHeight);
        vm.ContainerElements   = SelectedGarden.Elements;    // ссылка на коллекцию до Add()
        vm.ContainerGreenhouses = SelectedGarden.Greenhouses;
        SelectedGarden.Elements.Add(vm);
        SelectedElement = vm;
    }

    public ICommand AddGreenhouseCommand => field
        ??= new LambdaCommandAsync(AddGreenhouseAsync,
            () => SelectedGarden is not null);

    private async Task AddGreenhouseAsync()
    {
        if (SelectedGarden is null) return;
        var gh = await _gardenService.AddGreenhouseAsync(
            SelectedGarden.Id,
            $"Теплица {SelectedGarden.Greenhouses.Count + 1}",
            widthMeters: 6, heightMeters: 3, scale: 40);
        var vm = MapGreenhouse(gh, SelectedGarden.CanvasWidth, SelectedGarden.CanvasHeight);
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
        var entity = await _gardenService.Gardens
            .SelectMany(g => g.Elements)
            .FirstOrDefaultAsync(e => e.Id == SelectedElement.Id);
        if (entity is null) return;

        await _gardenService.DeleteElementAsync(entity);
        SelectedGarden.Elements.Remove(SelectedElement);
        SelectedElement = null;
    }

    public ICommand SaveElementPositionCommand => field
        ??= new LambdaCommandAsync(SaveElementPositionAsync,
            () => SelectedElement is not null);

    private async Task SaveElementPositionAsync()
    {
        if (SelectedElement is null) return;
        var entity = await _gardenService.Gardens
            .SelectMany(g => g.Elements)
            .FirstOrDefaultAsync(e => e.Id == SelectedElement.Id);
        if (entity is null) return;

        entity.X = SelectedElement.X;
        entity.Y = SelectedElement.Y;
        entity.DisplayWidth = SelectedElement.Width;
        entity.DisplayHeight = SelectedElement.Height;
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
        var entity = await _gardenService.Gardens
            .SelectMany(g => g.Greenhouses)
            .FirstOrDefaultAsync(gh => gh.Id == vm.Id);
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

        var entity = await _gardenService.Gardens
            .SelectMany(g => g.Elements)
            .FirstOrDefaultAsync(e => e.Id == SelectedElement.Id);
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
        var entity = await _gardenService.Gardens
            .SelectMany(g => g.Elements)
            .Include(e => e.PlantingSpots)
            .FirstOrDefaultAsync(e => e.Id == EditingGridElement.Id);
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
            async p => await ChangeSpotAsync(p as PlantingSpotFromViewModel, new ReservedSpotState()),
            _ => true);

    public ICommand PlantSpotCommand => field
        ??= new LambdaCommandAsync(
            async p => await PlantSpotAsync(p as PlantingSpotFromViewModel),
            _ => true);

    private async Task PlantSpotAsync(PlantingSpotFromViewModel? spotVm)
    {
        if (spotVm is null) return;
        var entity = await LoadSpotEntityAsync(spotVm.Id);
        if (entity is null) return;

        var newState = new PlantedSpotState();
        if (!entity.State.CanTransitionTo(newState)) return;

        await _gardenService.ChangeSpotStateAsync(
            entity, newState,
            string.IsNullOrWhiteSpace(NewPlantingLabel) ? null : NewPlantingLabel,
            NewPlantingDate);

        UpdateSpotVm(spotVm, newState, NewPlantingDate,
            string.IsNullOrWhiteSpace(NewPlantingLabel) ? null : NewPlantingLabel);
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
        => await _gardenService.Gardens
            .SelectMany(g => g.Elements)
            .SelectMany(e => e.PlantingSpots)
            .FirstOrDefaultAsync(s => s.Id == spotId);

    // ─────────────────────────────────────
    //  Вспомогательные методы маппинга
    // ─────────────────────────────────────

    private static GardenFromViewModel MapGarden(Garden g)
    {
        var elements = new ObservableCollection<GardenElementFromViewModel>(
            g.Elements.Select(e => MapElement(e, g.CanvasWidth, g.CanvasHeight)));

        var greenhouses = new ObservableCollection<GreenhouseFromViewModel>(
            g.Greenhouses.Select(gh => MapGreenhouse(gh, g.CanvasWidth, g.CanvasHeight)));

        // Каждый элемент получает ссылки на коллекции братских объектов
        // для проверки коллизий в code-behind во время drag/resize.
        foreach (var el in elements)
        {
            el.ContainerElements   = elements;
            el.ContainerGreenhouses = greenhouses;
        }

        return new GardenFromViewModel
        {
            Id = g.Id,
            Name = g.Name,
            WidthMeters = g.WidthMeters,
            HeightMeters = g.HeightMeters,
            CanvasWidth = g.CanvasWidth,
            CanvasHeight = g.CanvasHeight,
            Address = g.Address,
            Note = g.Note,
            Elements = elements,
            Greenhouses = greenhouses
        };
    }

    private static GardenElementFromViewModel MapElement(GardenElement e,
        double containerW = 0, double containerH = 0)
    {
        var state = e.State;
        return new GardenElementFromViewModel
        {
            Id = e.Id,
            Name = e.Name,
            ElementType = e.GetType().Name,
            ContainerCanvasWidth  = containerW,
            ContainerCanvasHeight = containerH,
            X = e.X,
            Y = e.Y,
            Width = e.DisplayWidth,
            Height = e.DisplayHeight,
            Rotation = e.Rotation,
            AreaSquareMeters = e.AreaSquareMeters,
            StateTypeName = e.StateTypeName,
            StateDisplayName = state.DisplayName,
            StateColor = state.StatusColor,
            CanAddPlanting = state.CanAddPlanting,
            CanModifyGrid = state.CanModifyGrid,
            GridRows = e.GridRows,
            GridColumns = e.GridColumns,
            SoilType = e.SoilType,
            Note = e.Note,
            Orientation = (e as Bed)?.Orientation,
            CoverMaterial = (e as ColdFrame)?.CoverMaterial,
            Shape = (e as FlowerBed)?.Shape,
            PlantingSpots = new ObservableCollection<PlantingSpotFromViewModel>(
                e.PlantingSpots.Select(MapSpot))
        };
    }

    private static GreenhouseFromViewModel MapGreenhouse(Greenhouse gh,
        double containerW = 0, double containerH = 0)
    {
        var state = gh.State;
        return new GreenhouseFromViewModel
        {
            Id = gh.Id,
            Name = gh.Name,
            ContainerCanvasWidth  = containerW,
            ContainerCanvasHeight = containerH,
            X = gh.X,
            Y = gh.Y,
            DisplayWidth = gh.DisplayWidth,
            DisplayHeight = gh.DisplayHeight,
            Rotation = gh.Rotation,
            WidthMeters = gh.WidthMeters,
            HeightMeters = gh.HeightMeters,
            InnerCanvasWidth = gh.CanvasWidth,
            InnerCanvasHeight = gh.CanvasHeight,
            StateTypeName = gh.StateTypeName,
            StateDisplayName = state.DisplayName,
            StateColor = state.StatusColor,
            Material = gh.Material,
            Note = gh.Note,
            InnerElements = new ObservableCollection<GardenElementFromViewModel>(
                gh.Elements.Select(e => MapElement(e, gh.CanvasWidth, gh.CanvasHeight)))
        };
    }

    private static PlantingSpotFromViewModel MapSpot(PlantingSpot s)
        => new()
        {
            Id           = s.Id,
            Row          = s.Row,
            Column       = s.Column,
            StateTypeName = s.StateTypeName,   // computed props (CellColor, CanGoTo* …) рассчитаются сами
            SeedlingInfoId = s.SeedlingInfoId,
            PlantedDate  = s.PlantedDate,
            PlantLabel   = s.Note,             // Note хранит «что посажено»
            Note         = s.Note
        };

    private static void RebuildSpotsVm(GardenElementFromViewModel vm, GardenElement entity)
    {
        vm.GridRows = entity.GridRows;
        vm.GridColumns = entity.GridColumns;
        vm.PlantingSpots = new ObservableCollection<PlantingSpotFromViewModel>(
            entity.PlantingSpots.Select(MapSpot));
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
