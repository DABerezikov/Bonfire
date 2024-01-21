using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seedling: Entity
{
    public virtual Plant Plant { get; set; }
    public virtual SeedlingInfo SeedlingInfo { get; set; }
    public int SeedlingInfoId { get; set; }

}