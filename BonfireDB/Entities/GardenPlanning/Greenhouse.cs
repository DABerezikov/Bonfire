using Bonfire.Domain.Garden.States;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonfireDB.Entities.GardenPlanning;

// Теплица — наследник GardenPlot: сама является контейнером (TPH "Greenhouse")
// и одновременно позиционируется на родительском GardenPlot.
public class Greenhouse : GardenPlot
{
    // --- Позиция на родительском участке ---
    public int ParentPlotId { get; set; }
    public virtual GardenPlot ParentPlot { get; set; } = null!;

    public double X { get; set; }
    public double Y { get; set; }
    public double DisplayWidth { get; set; } = 200;
    public double DisplayHeight { get; set; } = 120;
    public double Rotation { get; set; }

    // --- Состояние (полиморфное без enum) ---
    public string StateTypeName { get; set; } = nameof(PlannedState);

    [NotMapped]
    public GardenElementState State => GardenElementState.From(StateTypeName);

    public void TransitionTo(GardenElementState newState)
    {
        StateTypeName = State.TransitionTo(newState).Match(
            state => state.GetType().Name,
            error => throw new InvalidOperationException(error));
    }

    public bool IsLocked { get; set; }

    public string? Material { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
