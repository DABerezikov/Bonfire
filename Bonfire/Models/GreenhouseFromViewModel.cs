using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Infrastructure;
using BonfireDB.Entities.GardenPlanning.States;

namespace Bonfire.Models;

public class GreenhouseFromViewModel : INotifyPropertyChanged
{
    public int Id { get; set; }

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    // --- Размеры родительского контейнера (для проверки границ при drag/resize) ---
    public double ContainerCanvasWidth  { get; set; }
    public double ContainerCanvasHeight { get; set; }

    // --- Позиция на родительском Canvas ---
    private double _x;
    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }

    private double _y;
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }

    private double _displayWidth = 200;
    public double DisplayWidth
    {
        get => _displayWidth;
        set { _displayWidth = value; OnPropertyChanged(); }
    }

    private double _displayHeight = 120;
    public double DisplayHeight
    {
        get => _displayHeight;
        set { _displayHeight = value; OnPropertyChanged(); }
    }

    public double Rotation { get; set; }

    // --- Внутренние размеры (собственный Canvas) ---
    private double _widthMeters;
    public double WidthMeters
    {
        get => _widthMeters;
        set { _widthMeters = value; OnPropertyChanged(); }
    }

    private double _heightMeters;
    public double HeightMeters
    {
        get => _heightMeters;
        set { _heightMeters = value; OnPropertyChanged(); }
    }

    private double _innerCanvasWidth;
    public double InnerCanvasWidth
    {
        get => _innerCanvasWidth;
        set { _innerCanvasWidth = value; OnPropertyChanged(); }
    }

    private double _innerCanvasHeight;
    public double InnerCanvasHeight
    {
        get => _innerCanvasHeight;
        set { _innerCanvasHeight = value; OnPropertyChanged(); }
    }

    // --- Масштаб холста (синхронизируется из VM) ---
    private double _canvasZoom = 1.0;
    public double CanvasZoom
    {
        get => _canvasZoom;
        set
        {
            _canvasZoom = value;
            OnPropertyChanged(nameof(AdaptiveFontSize));
            OnPropertyChanged(nameof(AdaptiveFontSizeSmall));
            OnPropertyChanged(nameof(AdaptiveFontSizeTiny));
        }
    }

    // Адаптивные шрифты: буфер компенсации растёт с уменьшением базового размера
    public double AdaptiveFontSize      => CanvasConstants.AdaptiveFont(11.0, _canvasZoom);
    public double AdaptiveFontSizeSmall => CanvasConstants.AdaptiveFont(10.0, _canvasZoom);
    public double AdaptiveFontSizeTiny  => CanvasConstants.AdaptiveFont( 9.0, _canvasZoom);

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
    /// При изменении автоматически пересчитываются DisplayName, Color и CanGoTo*
    /// (по образцу GardenElementFromViewModel).
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

    // --- Допустимые переходы (для IsEnabled кнопок) ---
    public bool CanGoToPlanned  => GardenElementState.From(_stateTypeName).CanTransitionTo(new PlannedState());
    public bool CanGoToPrepared => GardenElementState.From(_stateTypeName).CanTransitionTo(new PreparedState());
    public bool CanGoToActive   => GardenElementState.From(_stateTypeName).CanTransitionTo(new ActiveState());
    public bool CanGoToFallow   => GardenElementState.From(_stateTypeName).CanTransitionTo(new FallowState());
    public bool CanGoToResting  => GardenElementState.From(_stateTypeName).CanTransitionTo(new RestingState());
    public bool CanGoToArchived => GardenElementState.From(_stateTypeName).CanTransitionTo(new ArchivedState());

    public string? Material { get; set; }
    public string? Note { get; set; }

    // --- Братские объекты на родительском Canvas (для проверки коллизий при drag) ---
    public ObservableCollection<GardenElementFromViewModel>? ContainerElements   { get; set; }
    public ObservableCollection<GreenhouseFromViewModel>?    ContainerGreenhouses { get; set; }

    // --- Элементы внутри теплицы ---
    public ObservableCollection<GardenElementFromViewModel> InnerElements { get; set; } = [];

    public double AreaSquareMeters => Math.Round(_widthMeters * _heightMeters, 1);

    public static string TypeFill => "#F3E5F5";
    public static string TypeColor => "#7B1FA2";
    public static string TypeLabel => "Теплица";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
