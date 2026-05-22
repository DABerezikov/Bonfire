using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seedling: Entity
{
    public Plant Plant { get; set; } = null!;
    public double Weight { get; set; }
    public double Quantity { get; set; }
    public int SeedId { get; set; }

    /// <summary>
    /// Сколько взошедших ростков уже высажено в грядки.
    /// Доступно к высадке = (взошедшие живые) − PlantedOut. Quantity (посеяно) не меняется.
    /// </summary>
    public int PlantedOut { get; set; }

    /// <summary>
    /// True — это «высаженная партия» (рассада уже в грядке), создаётся при посадке в ячейку.
    /// Такая рассада не доступна к повторной посадке (только к пересадке — пока не реализовано).
    /// </summary>
    public bool IsPlantedInBed { get; set; }

    // Метаданные партии (дублируют SeedlingInfos[0] для удобного доступа без загрузки коллекции)
    public DateTime? LandingDate { get; set; }
    public string? LunarPhase { get; set; }
    public string? SeedlingSource { get; set; }
    public string? PlantPlace { get; set; }

    public List<SeedlingInfo> SeedlingInfos { get; set; } = [];
}