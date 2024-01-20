using System;

namespace Bonfire.Models;

public class SeedsFromViewModel
{
    private double? _AmountSeedsQuantity;

    private double? _AmountSeedsWeight;
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }
    public DateTime ExpirationDate { get; set; }
    public double? WeightPack { get; set; }
    public double? QuantityPack { get; set; }

    public double? AmountSeedsWeight
    {
        get => _AmountSeedsWeight != 0.0 ? _AmountSeedsWeight : null;
        set => _AmountSeedsWeight = value;
    }

    public double? AmountSeedsQuantity
    {
        get => _AmountSeedsQuantity != 0.0 ? _AmountSeedsQuantity : null;
        set => _AmountSeedsQuantity = value;
    }

    public bool IsStillGood =>
        DateTime.Now.Year - ExpirationDate.Year <= 0 && DateTime.Now.Year - ExpirationDate.Year > -1;

    public bool IsOld => DateTime.Now.Year - ExpirationDate.Year > 0;
}