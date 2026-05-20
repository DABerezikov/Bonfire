using BonfireDB.Entities.Base;

namespace BonfireDB.Entities.GardenPlanning;

// Базовый контейнер с реальными размерами и холстом для элементов.
// Garden и Greenhouse наследуют этот класс (TPH).
public abstract class GardenPlot : NamedEntity
{
    /// <summary>Ширина участка в метрах</summary>
    public double WidthMeters { get; set; }

    /// <summary>Высота (глубина) участка в метрах</summary>
    public double HeightMeters { get; set; }

    /// <summary>Ширина холста в пикселях (WidthMeters × Scale)</summary>
    public double CanvasWidth { get; set; }

    /// <summary>Высота холста в пикселях (HeightMeters × Scale)</summary>
    public double CanvasHeight { get; set; }

    public string? Note { get; set; }

    public virtual List<GardenElement> Elements { get; set; } = [];
    public virtual List<Greenhouse> Greenhouses { get; set; } = [];
}
