using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Plant : NamedEntity
{
    public virtual PlantCulture PlantCulture { get; set; }
    public virtual PlantSort PlantSort { get; set; }
}