using System;

namespace Bonfire.Models;

public class SeedsFromViewModel
{
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int? WeightPack { get; set; }
    public int? QuantityPack { get; set; }
    public int? AmountSeedsWeight { get; set; }
    public int? AmountSeedsQuantity { get; set; }
}