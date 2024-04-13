using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Soil : Entity
{
    public DateTime ReplacementDate { get; set; }
    public Seedbed Seedbed { get; set; }
    public List<Treatment>? Treatments { get; set; }

}