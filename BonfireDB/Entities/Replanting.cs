using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Replanting : Entity
{
    /// <summary> Дата пересадки </summary>
    public DateTime ReplantingDate { get; set; }

    /// <summary> Объем горшка, л </summary>
    public double PotVolume { get;set; }

    /// <summary> Поле комментариев </summary>
    public string ReplantingNote { get; set; }

    public SeedlingInfo SeedlingInfo { get; set; }
}