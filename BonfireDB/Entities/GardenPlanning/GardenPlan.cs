using BonfireDB.Entities.Base;

namespace BonfireDB.Entities.GardenPlanning;

public class GardenPlan : NamedEntity
{
    public int Year { get; set; }
    public string? Description { get; set; }
    public virtual List<Garden> Gardens { get; set; } = [];
}
