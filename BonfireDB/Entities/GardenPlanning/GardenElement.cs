using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning.States;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonfireDB.Entities.GardenPlanning;

// Абстрактный элемент, размещаемый на GardenPlot (TPH).
public abstract class GardenElement : NamedEntity
{
    // --- Родительский контейнер (Garden или Greenhouse) ---
    public int PlotId { get; set; }
    public virtual GardenPlot Plot { get; set; } = null!;

    // --- Позиция и отображение на холсте ---
    public double X { get; set; }
    public double Y { get; set; }
    public double DisplayWidth { get; set; } = 120;
    public double DisplayHeight { get; set; } = 80;
    public double Rotation { get; set; }

    // --- Реальная площадь (пересчитывается при ресайзе) ---
    public double AreaSquareMeters { get; set; }

    // --- Состояние (полиморфное без enum) ---
    public string StateTypeName { get; set; } = nameof(PlannedState);

    [NotMapped]
    public GardenElementState State => GardenElementState.From(StateTypeName);

    public void TransitionTo(GardenElementState newState)
    {
        if (!State.CanTransitionTo(newState))
            throw new InvalidOperationException(
                $"Переход «{State.DisplayName}» → «{newState.DisplayName}» запрещён");
        StateTypeName = newState.GetType().Name;
    }

    // --- Сетка внутренней планировки ---
    public int GridRows { get; set; } = 1;
    public int GridColumns { get; set; } = 1;

    public string? SoilType { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual List<PlantingSpot> PlantingSpots { get; set; } = [];
}
