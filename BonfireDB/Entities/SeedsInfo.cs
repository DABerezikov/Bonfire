using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class SeedsInfo: Entity
{
    /// <summary>
    /// Вес семян в упаковке, грамм
    /// </summary>
    public double WeightPack { get; set; }
    /// <summary>
    /// Количество семян в упаковке, штук
    /// </summary>
    public double QuantityPack { get; set; }
    /// <summary>
    /// Дата покупки
    /// </summary>
    public DateTime PurchaseDate { get; set; }
    /// <summary>
    /// Срок годности семян
    /// </summary>
    public DateTime ExpirationDate { get; set; }
    /// <summary>
    /// Стоимость упаковки семян, руб
    /// </summary>
    public decimal CostPack { get; set; }

    /// <summary>
    /// Комментарий при удалении
    /// </summary>
    public string? DisposeComment { get; set; }
    /// <summary>
    /// Количество семян штук
    /// </summary>
    public double AmountSeeds { get; set; }
    /// <summary>
    /// Количество семян грамм
    /// </summary>
    public double? AmountSeedsWeight { get; set; }
    /// <summary>
    /// Источник семян
    /// </summary>
    public string? SeedSource { get; set; }

    /// <summary>
    /// Примечание
    /// </summary>
    public string? Note { get; set; }

    
    public Seed Seed { get; set; }
}