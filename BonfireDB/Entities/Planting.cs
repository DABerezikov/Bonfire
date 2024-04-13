using System.ComponentModel.DataAnnotations.Schema;
using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Planting : Entity
{
    public Plant Plant { get; set; }
    public int SeedlingId { get; set; }
    [ForeignKey(nameof(Planting))]
    public PlantingInfo PlantingInfo { get; set; }
    public List<Seedbed> Seedbeds { get; set; }
}