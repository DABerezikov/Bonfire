using System;

namespace Bonfire.Models;

public class SeedsFromViewModel
{
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }
    public DateTime ExpirationDate { get; set; }


    public double? WeightPack
    {
        get => field != 0.0 ? field : null;
        set;
    }

    public double? QuantityPack
    {
        get => field != 0.0 ? field : null;
        set;
    }

    public double? AmountSeedsWeight
    {
        get => field != 0.0 ? field : null;
        set;
    }

    public double? AmountSeedsQuantity
    {
        get => field != 0.0 ? field : null;
        set;
    }

    public bool IsStillGood =>
        DateTime.Now.Year - ExpirationDate.Year <= 0 && DateTime.Now.Year - ExpirationDate.Year > -1;

    public bool IsOld => DateTime.Now.Year - ExpirationDate.Year > 0;
}

