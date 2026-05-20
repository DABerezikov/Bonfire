using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace Bonfire.Models;

public class PlantingSpotFromViewModel : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }

    private string _stateTypeName = "EmptySpotState";

    /// <summary>
    /// Тип состояния ячейки. При изменении автоматически пересчитываются
    /// все вычислимые свойства: цвета, DisplayName, CanGoTo*.
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
            OnPropertyChanged(nameof(CellColor));
            OnPropertyChanged(nameof(CellBorderColor));
            OnPropertyChanged(nameof(CanPlant));
            OnPropertyChanged(nameof(CanGoToReserved));
            OnPropertyChanged(nameof(CanGoToPlanted));
            OnPropertyChanged(nameof(CanGoToHarvested));
            OnPropertyChanged(nameof(CanGoToDead));
            OnPropertyChanged(nameof(CanGoToEmpty));
        }
    }

    // --- Вычислимые из StateTypeName ---
    public string StateDisplayName => PlantingSpotState.From(_stateTypeName).DisplayName;
    public string CellColor        => PlantingSpotState.From(_stateTypeName).CellColor;
    public string CellBorderColor  => PlantingSpotState.From(_stateTypeName).CellBorderColor;
    public bool   CanPlant         => PlantingSpotState.From(_stateTypeName).CanPlant;

    // --- Допустимые переходы ---
    public bool CanGoToReserved  => PlantingSpotState.From(_stateTypeName).CanTransitionTo(new ReservedSpotState());
    public bool CanGoToPlanted   => PlantingSpotState.From(_stateTypeName).CanTransitionTo(new PlantedSpotState());
    public bool CanGoToHarvested => PlantingSpotState.From(_stateTypeName).CanTransitionTo(new HarvestedSpotState());
    public bool CanGoToDead      => PlantingSpotState.From(_stateTypeName).CanTransitionTo(new DeadSpotState());
    public bool CanGoToEmpty     => PlantingSpotState.From(_stateTypeName).CanTransitionTo(new EmptySpotState());

    // --- Выделение ---
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    // --- Данные посадки ---

    private string? _plantLabel;
    /// <summary>
    /// Что посажено (название, заполняется пользователем при посадке).
    /// Хранится в PlantingSpot.Note.
    /// </summary>
    public string? PlantLabel
    {
        get => _plantLabel;
        set { _plantLabel = value; OnPropertyChanged(); }
    }

    private DateTime? _plantedDate;
    public DateTime? PlantedDate
    {
        get => _plantedDate;
        set { _plantedDate = value; OnPropertyChanged(); }
    }

    public string? Note { get; set; }
    public int? SeedlingInfoId { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
