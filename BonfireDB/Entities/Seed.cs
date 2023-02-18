using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seed : Entity
{
    public virtual Plant Plant { get; set; }
    public virtual SeedsInfo SeedsInfo { get; set; }

}