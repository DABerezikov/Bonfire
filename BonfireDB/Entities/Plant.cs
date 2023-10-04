using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Plant : Entity
{
    public virtual PlantCulture PlantCulture { get; set; }
    public virtual PlantSort PlantSort { get; set; }
}