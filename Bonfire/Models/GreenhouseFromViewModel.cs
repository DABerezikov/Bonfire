using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    public double WidthMeters { get; set; }
    public double HeightMeters { get; set; }
    public double InnerCanvasWidth { get; set; }
    public double InnerCanvasHeight { get; set; }

    // --- Выделение ---
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    // --- Состояние ---
    public string StateTypeName { get; set; } = "PlannedState";
    public string StateDisplayName { get; set; } = "Запланирована";
    public string StateColor { get; set; } = "#9E9E9E";

    public string? Material { get; set; }
    public string? Note { get; set; }

    // --- Элементы внутри теплицы ---
    public ObservableCollection<GardenElementFromViewModel> InnerElements { get; set; } = [];

    public static string TypeFill => "#F3E5F5";
    public static string TypeColor => "#7B1FA2";
    public static string TypeLabel => "Теплица";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
