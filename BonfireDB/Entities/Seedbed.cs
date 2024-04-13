using System.ComponentModel.DataAnnotations.Schema;
using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seedbed : NamedEntity
{
    public double AbscissaX { get; set; }
    public double OrdinateY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }


    public bool IsRegular { get; set; }
    public bool IsHotBed { get; set; }
    public bool IsAutoWatering { get; set; }

    [ForeignKey(nameof(Seedbed))]
    public Soil Soil { get; set; }
    public List<Planting> Plantings { get; set; }

}