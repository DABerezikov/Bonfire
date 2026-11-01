using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bonfire.Models;

public class GardenFromViewModel : INotifyPropertyChanged
{
    private double _canvasHeight;

    private double _canvasWidth;

    private double _heightMeters;

    private string _name = "";

    private double _widthMeters;
    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public double WidthMeters
    {
        get => _widthMeters;
        set
        {
            _widthMeters = value;
            OnPropertyChanged();
        }
    }

    public double HeightMeters
    {
        get => _heightMeters;
        set
        {
            _heightMeters = value;
            OnPropertyChanged();
        }
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            _canvasWidth = value;
            OnPropertyChanged();
        }
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            _canvasHeight = value;
            OnPropertyChanged();
        }
    }

    public string? Address { get; set; }
    public string? Note { get; set; }

    public ObservableCollection<GardenElementFromViewModel> Elements { get; set; } = [];
    public ObservableCollection<GreenhouseFromViewModel> Greenhouses { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}