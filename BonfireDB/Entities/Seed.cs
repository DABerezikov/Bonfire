using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seed : Entity
{
    public virtual Plant Plant { get; set; } = null!;
    public virtual SeedsInfo SeedsInfo { get; set; } = null!;
    public int SeedsInfoId { get; set; }

}