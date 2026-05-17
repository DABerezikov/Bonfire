using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bonfire.Models;

public class SeedlingFromViewModel
{
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }

    public double? Weight
    {
        get => field != 0 ? field : null;
        set;
    }

    public double? Quantity
    {
        get => field != 0 ? field : null;
        set;
    }

    public DateTime? LandingData { get; set; }
    public DateTime? ReplantingData { get; set; }
    public string? SeedlingMoonPhase { get; set; }
    public bool? IsDead { get; set; }
    public ObservableCollection<SeedlingInfoFromViewModel>? SeedlingInfos { get; set; }

    public int? MinGerminate
    {
        get
        {
            if (SeedlingInfos!.Count < 1) return null;
            var minDate = SeedlingInfos.Min(d => d.GerminationData);
            return (minDate -  LandingData)!.Value.Days;
        }
    }

    public int? MaxGerminate
    {
        get
        {
            if (SeedlingInfos!.Count < 1) return null;
            var maxDate = SeedlingInfos.Max(d => d.GerminationData);
            return (maxDate - LandingData)!.Value.Days;
        }
    }

    public int CountGerminate => SeedlingInfos != null && SeedlingInfos.Count != 0 ? SeedlingInfos.Count : 0;

    public int? Balance => SeedlingInfos == null ? null : CountGerminate - SeedlingInfos.Select(s => s).Skip(1).Count(s => s.IsDead == true);
}