using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Treatment : Entity
{
    /// <summary> Дата обработки </summary>
    public DateTime TreatmentDate { get; set; }

    /// <summary> Препарат для обработки </summary>
    public string Product { get; set; }

    /// <summary> Способ обработки </summary>
    public string TreatmentMethod { get; set; }

    public SeedlingInfo SeedlingInfo { get; set; }
}