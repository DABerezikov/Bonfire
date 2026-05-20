using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seedling: Entity
{
    public Plant Plant { get; set; } = null!;
    public double Weight { get; set; }
    public double Quantity { get; set; }
    public int SeedId { get; set; }

    // Метаданные партии (дублируют SeedlingInfos[0] для удобного доступа без загрузки коллекции)
    public DateTime? LandingDate { get; set; }
    public string? LunarPhase { get; set; }
    public string? SeedlingSource { get; set; }
    public string? PlantPlace { get; set; }

    public List<SeedlingInfo> SeedlingInfos { get; set; } = [];
}