using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record LifeCycleFieldValues
{
    public int Mode { get; init; } = -1;
    public string FirstFlower { get; init; } = "1";
    public string FirstHarvest { get; init; } = "1";
    public string FlowerStartMin { get; init; } = "";
    public string FlowerStartMax { get; init; } = "";
    public string FlowerEndMin { get; init; } = "";
    public string FlowerEndMax { get; init; } = "";
    public string FruitStartMin { get; init; } = "";
    public string FruitStartMax { get; init; } = "";
    public string FruitEndMin { get; init; } = "";
    public string FruitEndMax { get; init; } = "";
    public string FlowerAnnualStart { get; init; } = "";
    public string FlowerAnnualEnd { get; init; } = "";
    public string FruitAnnualStart { get; init; } = "";
    public string FruitAnnualEnd { get; init; } = "";
    public bool FlowerNextYear { get; init; }
    public bool FruitNextYear { get; init; }
}

public sealed class LifeCycleFields(LifeCycleFieldValues values) : ViewModel, IDataErrorInfo
{
    public LifeCycleFieldValues Values { get; private set; } = values;
    public bool ShowDetails => Mode >= 0;

    public int Mode
    {
        get => Values.Mode;
        set => Update(Values with { Mode = value });
    }

    public string FirstFlower
    {
        get => Values.FirstFlower;
        set => Update(Values with { FirstFlower = value });
    }

    public string FirstHarvest
    {
        get => Values.FirstHarvest;
        set => Update(Values with { FirstHarvest = value });
    }

    public string FlowerStartMin
    {
        get => Values.FlowerStartMin;
        set => Update(Values with { FlowerStartMin = value });
    }

    public string FlowerStartMax
    {
        get => Values.FlowerStartMax;
        set => Update(Values with { FlowerStartMax = value });
    }

    public string FlowerEndMin
    {
        get => Values.FlowerEndMin;
        set => Update(Values with { FlowerEndMin = value });
    }

    public string FlowerEndMax
    {
        get => Values.FlowerEndMax;
        set => Update(Values with { FlowerEndMax = value });
    }

    public string FruitStartMin
    {
        get => Values.FruitStartMin;
        set => Update(Values with { FruitStartMin = value });
    }

    public string FruitStartMax
    {
        get => Values.FruitStartMax;
        set => Update(Values with { FruitStartMax = value });
    }

    public string FruitEndMin
    {
        get => Values.FruitEndMin;
        set => Update(Values with { FruitEndMin = value });
    }

    public string FruitEndMax
    {
        get => Values.FruitEndMax;
        set => Update(Values with { FruitEndMax = value });
    }

    public string FlowerAnnualStart
    {
        get => Values.FlowerAnnualStart;
        set => Update(Values with { FlowerAnnualStart = value });
    }

    public string FlowerAnnualEnd
    {
        get => Values.FlowerAnnualEnd;
        set => Update(Values with { FlowerAnnualEnd = value });
    }

    public string FruitAnnualStart
    {
        get => Values.FruitAnnualStart;
        set => Update(Values with { FruitAnnualStart = value });
    }

    public string FruitAnnualEnd
    {
        get => Values.FruitAnnualEnd;
        set => Update(Values with { FruitAnnualEnd = value });
    }

    public bool FlowerNextYear
    {
        get => Values.FlowerNextYear;
        set => Update(Values with { FlowerNextYear = value });
    }

    public bool FruitNextYear
    {
        get => Values.FruitNextYear;
        set => Update(Values with { FruitNextYear = value });
    }

    public string Error => PlanningInputParser.Error(Build());
    public string this[string name] => Error;

    public Result<Option<LifeCycleProfile>> Build()
    {
        if (Mode == -1) return Result.Success(Option.None<LifeCycleProfile>());
        if (!int.TryParse(FirstFlower, out var flower) || flower <= 0 || !int.TryParse(FirstHarvest, out var fruit) ||
            fruit <= 0)
            return Result.Failure<Option<LifeCycleProfile>>("Первый сезон должен быть положительным целым номером");
        return PlanningInputParser.Range(FlowerStartMin, FlowerStartMax, "Начало цветения").Bind(fs =>
            PlanningInputParser.Range(FlowerEndMin, FlowerEndMax, "Окончание цветения").Bind(fe =>
                PlanningInputParser.Range(FruitStartMin, FruitStartMax, "Начало плодоношения").Bind(rs =>
                    PlanningInputParser.Range(FruitEndMin, FruitEndMax, "Окончание плодоношения").Bind(re =>
                        PlanningInputParser.Annual(FlowerAnnualStart, FlowerAnnualEnd, FlowerNextYear).Bind(fa =>
                            PlanningInputParser.Annual(FruitAnnualStart, FruitAnnualEnd, FruitNextYear).Bind(ra =>
                                new LifeCycleProfile
                                    {
                                        Kind = (LifeCycleKind)Mode, FirstFloweringSeason = flower,
                                        FirstHarvestSeason = fruit,
                                        FloweringFirstYear = new PhenologyOffsets { Start = fs, End = fe },
                                        FruitingFirstYear = new PhenologyOffsets { Start = rs, End = re },
                                        FloweringLater = fa, FruitingLater = ra
                                    }
                                    .Validate().Bind(profile => Result.Success(Option.Some(profile)))))))));
    }

    public static LifeCycleFieldValues From(Option<LifeCycleProfile> profile) => profile.Match(x =>
        new LifeCycleFieldValues
        {
            Mode = (int)x.Kind, FirstFlower = x.FirstFloweringSeason.ToString(),
            FirstHarvest = x.FirstHarvestSeason.ToString(),
            FlowerStartMin = Bound(x.FloweringFirstYear.Start, false),
            FlowerStartMax = Bound(x.FloweringFirstYear.Start, true),
            FlowerEndMin = Bound(x.FloweringFirstYear.End, false),
            FlowerEndMax = Bound(x.FloweringFirstYear.End, true),
            FruitStartMin = Bound(x.FruitingFirstYear.Start, false),
            FruitStartMax = Bound(x.FruitingFirstYear.Start, true),
            FruitEndMin = Bound(x.FruitingFirstYear.End, false),
            FruitEndMax = Bound(x.FruitingFirstYear.End, true),
            FlowerAnnualStart = Annual(x.FloweringLater, false), FlowerAnnualEnd = Annual(x.FloweringLater, true),
            FruitAnnualStart = Annual(x.FruitingLater, false), FruitAnnualEnd = Annual(x.FruitingLater, true),
            FlowerNextYear = x.FloweringLater.Match(y => y.EndInNextYear, () => false),
            FruitNextYear = x.FruitingLater.Match(y => y.EndInNextYear, () => false)
        }, () => new LifeCycleFieldValues());

    private static string Bound(Option<DayRange> value, bool end) =>
        value.Match(x => (end ? x.Max : x.Min).ToString(), () => "");

    private static string Annual(Option<AnnualDateWindow> value, bool end) =>
        value.Match(x => end ? $"{x.EndDay:00}.{x.EndMonth:00}" : $"{x.StartDay:00}.{x.StartMonth:00}", () => "");

    private void Update(LifeCycleFieldValues values, [CallerMemberName] string? name = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(name);
        OnPropertyChanged(nameof(ShowDetails));
        OnPropertyChanged(nameof(Error));
    }
}