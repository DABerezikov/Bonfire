using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class PlantingInfo : Entity
{
    public Planting Planting { get; set; }
    /// <summary> Список обработки </summary>
    public ICollection<Treatment>? Treatments { get; set; }
}