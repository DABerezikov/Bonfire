namespace Bonfire.Models;

/// <summary>Тип источника материала для посадки.</summary>
public enum PlantSourceKind { Seedling, Seed }

/// <summary>
/// Элемент пикера «что высаживаем»: партия рассады или пакет семян.
/// Label содержит имя растения и остаток; PlantName — только имя без количества.
/// </summary>
public class PlantSourceItem
{
    public PlantSourceKind Kind        { get; set; }

    /// <summary>Seedling.Id или Seed.Id в зависимости от Kind.</summary>
    public int             EntityId    { get; set; }

    /// <summary>«Культура Сорт» без количества — сохраняется в ячейку.</summary>
    public string          PlantName   { get; set; } = "";

    /// <summary>«Культура Сорт (N шт.)» или «... (N.N г.)» — отображается в ComboBox.</summary>
    public string          Label       { get; set; } = "";

    /// <summary>
    /// True — остаток измеряется в граммах (AmountSeedsWeight / Seedling.Weight).
    /// False — в штуках (AmountSeeds / Seedling.Quantity).
    /// </summary>
    public bool            IsWeightBased { get; set; }

    /// <summary>Остаток в единицах измерения: шт. или г.</summary>
    public double          AvailableQty { get; set; }

    public override string ToString() => Label;
}
