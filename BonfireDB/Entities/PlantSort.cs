using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class PlantSort : NamedEntity
{
    /// <summary>
    /// Описание сорта
    /// </summary>
    public string? Description { get; set; } 

    /// <summary>
    /// Производитель семян
    /// </summary>
    public Producer Producer { get; set; }

    /// <summary>
    /// Минимальное время вегетации, дней
    /// </summary>
    public int? MinGerminationTime { get; set; }

    /// <summary>
    /// Максимальное время вегетации, дней
    /// </summary>
    public int? MaxGerminationTime { get; set; }

    /// <summary>
    /// Возраст рассады на высадку, дней
    /// </summary>
    public int? AgeOfSeedlings { get; set; }

    /// <summary>
    /// Вегетационный период, дней
    /// </summary>
    public int? GrowingSeason { get; set; }

    /// <summary>
    /// Схема посадки, см
    /// </summary>
    public int? LandingPattern { get; set; }

    /// <summary>
    /// Высота растения, см
    /// </summary>
    public int? PlantHeight { get; set; }

    /// <summary>
    /// Цвет растения
    /// </summary>
    public string? PlantColor { get; set; }

    public ICollection<Plant> Plants { get; set; }
}