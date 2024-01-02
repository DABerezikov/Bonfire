using System;

namespace Bonfire.Models;

public class SeedsFromViewModel
{
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }
    public DateTime ExpirationDate { get; set; }
    public double? WeightPack { get; set; }
    public double? QuantityPack { get; set; }
    public double? AmountSeedsWeight { get; set; }
    public double? AmountSeedsQuantity { get; set; }

    public bool IsStillGood => (DateTime.Now.Year - ExpirationDate.Year) <= 0 && (DateTime.Now.Year - ExpirationDate.Year) > -1;
    public bool IsOld => (DateTime.Now.Year - ExpirationDate.Year) > 0;
}