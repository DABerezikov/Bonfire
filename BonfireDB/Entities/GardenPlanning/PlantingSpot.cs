using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonfireDB.Entities.GardenPlanning;

// Ячейка в сетке планировки элемента (одно место для одного растения).
public class PlantingSpot : Entity
{
    public int Row { get; set; }
    public int Column { get; set; }

    public int GardenElementId { get; set; }
    public virtual GardenElement GardenElement { get; set; } = null!;

    // Высаженная рассада (необязательно)
    public int? SeedlingInfoId { get; set; }
    public virtual SeedlingInfo? SeedlingInfo { get; set; }

    // Состояние ячейки (полиморфное без enum)
    public string StateTypeName { get; set; } = nameof(EmptySpotState);

    [NotMapped]
    public PlantingSpotState State => PlantingSpotState.From(StateTypeName);

    public void TransitionTo(PlantingSpotState newState)
    {
        if (!State.CanTransitionTo(newState))
            throw new InvalidOperationException(
                $"Переход «{State.DisplayName}» → «{newState.DisplayName}» запрещён");
        StateTypeName = newState.GetType().Name;
    }

    public DateTime? PlantedDate { get; set; }
    public DateTime? HarvestDate { get; set; }
    public string? Note { get; set; }
}
