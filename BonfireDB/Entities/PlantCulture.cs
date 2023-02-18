using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class PlantCulture : NamedEntity
{
    public string Class { get; set; }
    public ICollection<Plant> Plants { get; set; }
}