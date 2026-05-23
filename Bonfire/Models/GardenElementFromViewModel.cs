using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BonfireDB.Entities.GardenPlanning.States;

namespace Bonfire.Models;

public class GardenElementFromViewModel : INotifyPropertyChanged
{
    public int Id { get; set; }

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string ElementType { get; set; } = "";

    public string TypeLabel => ElementType switch
    {
        "Bed"            => "Грядка",
        "ColdFrame"      => "Парник",
        "FlowerBed"      => "Цветник",
        "OpenGroundArea" => "Открытый грунт",
        _                => ElementType
    };

    public string TypeColor => ElementType switch
    {
        "Bed"            => "#388E3C",
        "ColdFrame"      => "#1976D2",
        "FlowerBed"      => "#E91E63",
        "OpenGroundArea" => "#795548",
        _                => "#607D8B"
    };

    public string TypeFill => ElementType switch
    {
        "Bed"            => "#E8F5E9",
        "ColdFrame"      => "#E3F2FD",
        "FlowerBed"      => "#FCE4EC",
        "OpenGroundArea" => "#EFEBE9",
        _                => "#F5F5F5"
    };

    // --- Размеры контейнера (для проверки границ при drag/resize) ---
    public double ContainerCanvasWidth  { get; set; }
    public double ContainerCanvasHeight { get; set; }

    // --- Позиция на Canvas ---
    private double _x;
    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }

    private double _y;
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }

    private double _width = 36;
    public double Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); OnPropertyChanged(nameof(AreaSquareMeters)); }
    }

    private double _height = 80;
    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); OnPropertyChanged(nameof(AreaSquareMeters)); }
    }

    public double Rotation { get; set; }

    // Масштаб 40 пкс/м → 1 м² = 1600 пкс²
    public double AreaSquareMeters => Math.Round(_width * _height / 1600.0, 1);

    // --- Блокировка ---
    private bool _isLocked;
    public bool IsLocked
    {
        get => _isLocked;
        set { _isLocked = value; OnPropertyChanged(); }
    }

    // --- Выделение ---
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    // --- Состояние ---

    private string _stateTypeName = "PlannedState";
    /// <summary>
    /// Имя типа состояния. При изменении автоматически пересчитываются DisplayName,
    /// Color и все CanGoTo* — чтобы кнопки переходов сразу стали активными/неактивными.
    /// </summary>
    public string StateTypeName
    {
        get => _stateTypeName;
        set
        {
            if (_stateTypeName == value) return;
            _stateTypeName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StateDisplayName));
            OnPropertyChanged(nameof(StateColor));
            OnPropertyChanged(nameof(CanAddPlanting));
            OnPropertyChanged(nameof(CanModifyGrid));
            OnPropertyChanged(nameof(CanGoToPlanned));
            OnPropertyChanged(nameof(CanGoToPrepared));
            OnPropertyChanged(nameof(CanGoToActive));
            OnPropertyChanged(nameof(CanGoToFallow));
            OnPropertyChanged(nameof(CanGoToResting));
            OnPropertyChanged(nameof(CanGoToArchived));
        }
    }

    private string _stateDisplayName = "Запланирована";
    public string StateDisplayName
    {
        get => _stateDisplayName;
        set { _stateDisplayName = value; OnPropertyChanged(); }
    }

    private string _stateColor = "#9E9E9E";
    public string StateColor
    {
        get => _stateColor;
        set { _stateColor = value; OnPropertyChanged(); }
    }

    private bool _canAddPlanting;
    public bool CanAddPlanting
    {
        get => _canAddPlanting;
        set { _canAddPlanting = value; OnPropertyChanged(); }
    }

    private bool _canModifyGrid = true;
    public bool CanModifyGrid
    {
        get => _canModifyGrid;
        set { _canModifyGrid = value; OnPropertyChanged(); }
    }

    // --- Допустимые переходы (для IsEnabled кнопок) ---
    public bool CanGoToPlanned  => GardenElementState.From(_stateTypeName).CanTransitionTo(new PlannedState());
    public bool CanGoToPrepared => GardenElementState.From(_stateTypeName).CanTransitionTo(new PreparedState());
    public bool CanGoToActive   => GardenElementState.From(_stateTypeName).CanTransitionTo(new ActiveState());
    public bool CanGoToFallow   => GardenElementState.From(_stateTypeName).CanTransitionTo(new FallowState());
    public bool CanGoToResting  => GardenElementState.From(_stateTypeName).CanTransitionTo(new RestingState());
    public bool CanGoToArchived => GardenElementState.From(_stateTypeName).CanTransitionTo(new ArchivedState());

    // --- Ссылки для проверки коллизий в code-behind при drag/resize ---
    public ObservableCollection<GardenElementFromViewModel>? ContainerElements   { get; set; }
    public ObservableCollection<GreenhouseFromViewModel>?    ContainerGreenhouses { get; set; }

    // --- Сетка посадок ---
    private int _gridRows = 1;
    public int GridRows
    {
        get => _gridRows;
        set { _gridRows = value; OnPropertyChanged(); }
    }

    private int _gridColumns = 1;
    public int GridColumns
    {
        get => _gridColumns;
        set { _gridColumns = value; OnPropertyChanged(); }
    }

    private ObservableCollection<PlantingSpotFromViewModel> _plantingSpots = [];
    public ObservableCollection<PlantingSpotFromViewModel> PlantingSpots
    {
        get => _plantingSpots;
        set { _plantingSpots = value; OnPropertyChanged(); }
    }

    public string? SoilType { get; set; }
    public string? Note { get; set; }

    // --- Специфичные поля (по типу) ---
    public string? Orientation { get; set; }   // Bed
    public string? CoverMaterial { get; set; } // ColdFrame
    public string? Shape { get; set; }         // FlowerBed

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
