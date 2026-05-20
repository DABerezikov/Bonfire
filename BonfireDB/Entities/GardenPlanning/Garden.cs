namespace BonfireDB.Entities.GardenPlanning;

// Основной участок огорода — корневой контейнер (TPH дискриминатор "Garden").
public class Garden : GardenPlot
{
    public int GardenPlanId { get; set; }
    public virtual GardenPlan GardenPlan { get; set; } = null!;
    public string? Address { get; set; }
}
