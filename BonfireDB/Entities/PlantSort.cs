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
    public Producer Producer { get; set; } = null!;

    /// <summary>
    /// Минимальный срок прорастания, дней
    /// </summary>
    public int? MinGerminationTime { get; set; }

    /// <summary>
    /// Максимальный срок прорастания, дней
    /// </summary>
    public int? MaxGerminationTime { get; set; }

    /// <summary>
    /// Возраст рассады на высадку, дней
    /// </summary>
    public int? AgeOfSeedlings { get; set; }

    /// <summary>
    /// Минимальный вегетационный период, дней
    /// </summary>
    public int? GrowingSeason { get; set; }

    /// <summary>Максимальный вегетационный период, дней</summary>
    public int? MaxGrowingSeason { get; set; }

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

    /// <summary>Созревание</summary>
    public string? Maturity { get; set; }

    /// <summary>Максимальный возраст рассады, дней</summary>
    public int? MaxAgeOfSeedlings { get; set; }

    /// <summary>Максимальная высота, см</summary>
    public int? MaxPlantHeight { get; set; }

    /// <summary>Расстояние между рядами, см</summary>
    public int? LandingRowSpacing { get; set; }

    /// <summary>Урожайность, кг</summary>
    public double? YieldKg { get; set; }

    /// <summary>Минимальная масса плода, г</summary>
    public double? MinFruitWeight { get; set; }

    /// <summary>Максимальная масса плода, г</summary>
    public double? MaxFruitWeight { get; set; }

    /// <summary>Минимальный размер плода, см</summary>
    public int? MinFruitSize { get; set; }

    /// <summary>Максимальный размер плода, см</summary>
    public int? MaxFruitSize { get; set; }

    public ICollection<Plant> Plants { get; set; } = [];
}