using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record PerennialEditorValues(string Name, string EstablishmentYear, PlanningChoiceFromViewModel? Choice,
    RegionFromViewModel? Region, string EmergenceStart, string EmergenceEnd, LifeCycleFieldValues LifeCycle, ParameterSource ProfileSource);
public sealed class PerennialEditorDraft : ViewModel, IDataErrorInfo
{
    public PerennialEditorValues Values { get; private set; }
    public PerennialEditorValues Original { get; private set; }
    public Option<int> Id { get; private set; } = Option.None<int>();
    public int Version { get; private set; }
    public LifeCycleFields LifeCycle { get; private set; }
    public bool IsDirty => Values != Original;
    public PerennialEditorDraft(int year) : this(new PerennialEditorValues("", year.ToString(), null, null, "", "", new(), ParameterSource.Batch)) { }
    private PerennialEditorDraft(PerennialEditorValues values) { Values = Original = values; LifeCycle = new(values.LifeCycle); WireLife(); }
    public static PerennialEditorDraft Open(PerennialPlantingFromViewModel stored)
    {
        var value = stored.Definition;
        var region = new RegionFromViewModel(value.RegionId, RegionDefinition.Create(value.RegionName, value.TimeZoneId, RegionReference.Empty).ValueOrThrow());
        return new(new PerennialEditorValues(value.Name, value.EstablishmentYear.ToString(), new(value.CultureId.Value, value.CultureName, value.SortId.Value, value.SortName), region,
            value.FirstEmergence.Match(x => PlanningInputParser.Format(x.Start), () => ""), value.FirstEmergence.Match(x => PlanningInputParser.Format(x.End), () => ""),
            LifeCycleFields.From(Option.Some(value.Profile.Value)), value.Profile.Source)) { Id = Option.Some(stored.Id), Version = stored.Version };
    }
    public string Name { get => Values.Name; set => Update(Values with { Name = value }); }
    public string EstablishmentYear { get => Values.EstablishmentYear; set => Update(Values with { EstablishmentYear = value }); }
    public PlanningChoiceFromViewModel? Choice { get => Values.Choice; set => Update(Values with { Choice = value }); }
    public RegionFromViewModel? Region { get => Values.Region; set => Update(Values with { Region = value }); }
    public string EmergenceStart { get => Values.EmergenceStart; set => Update(Values with { EmergenceStart = value }); }
    public string EmergenceEnd { get => Values.EmergenceEnd; set => Update(Values with { EmergenceEnd = value }); }
    public void CopyProfile(SourcedValue<LifeCycleProfile> profile)
    {
        var fields = LifeCycleFields.From(Option.Some(profile.Value));
        LifeCycle = new(fields); WireLife(); Update(Values with { LifeCycle = fields, ProfileSource = profile.Source }, nameof(LifeCycle));
    }
    public Result<PerennialPlantingDefinition> Build()
    {
        if (Choice is not { } choice || Region is not { } region || !int.TryParse(EstablishmentYear, out var year))
            return Result.Failure<PerennialPlantingDefinition>("Выберите культуру/сорт, регион и год начала");
        return LifeCycle.Build().Bind(profile => profile.Match(life =>
            OptionalWindow(EmergenceStart, EmergenceEnd).Bind(emergence => CultureId.Create(choice.CultureId).Bind(c => SortId.Create(choice.SortId).Bind(s =>
                PerennialPlantingDefinition.Create(Name, year, c, s, choice.CultureName, choice.SortName, region.Id, region.Definition.Name.Value,
                    region.Definition.TimeZoneId, new(life, Values.ProfileSource), emergence)))),
            () => Result.Failure<PerennialPlantingDefinition>("Выберите двулетний/многолетний жизненный цикл")));
    }
    public void Accept(PerennialEditorValues submitted, PerennialPlantingFromViewModel saved)
    { Original = submitted; Id = Option.Some(saved.Id); Version = saved.Version; OnPropertyChanged(string.Empty); }
    internal static Result<Option<DateWindow>> OptionalWindow(string start, string end) => start.Length == 0 && end.Length == 0
        ? Result.Success(Option.None<DateWindow>()) : PlanningInputParser.Window(start, end).Bind(x => Result.Success(Option.Some(x)));
    public string Error => PlanningInputParser.Error(Build());
    public string this[string property] => Error;
    private void WireLife() => LifeCycle.PropertyChanged += (sender, args) =>
    { if (ReferenceEquals(sender, LifeCycle) && args.PropertyName == nameof(LifeCycleFields.Values)) Update(Values with { LifeCycle = LifeCycle.Values, ProfileSource = ParameterSource.Batch }, nameof(LifeCycle)); };
    private void Update(PerennialEditorValues values, [CallerMemberName] string? name = null)
    { if (Values == values) return; Values = values; OnPropertyChanged(nameof(Values)); OnPropertyChanged(name); OnPropertyChanged(nameof(IsDirty)); OnPropertyChanged(nameof(Error)); }
}

public sealed record PerennialSeasonValues(string FlowerStart, string FlowerEnd, string FruitStart, string FruitEnd);
public sealed class PerennialSeasonDraft : ViewModel, IDataErrorInfo
{
    public PerennialSeasonValues Values { get; private set; }
    public PerennialSeasonValues Original { get; private set; }
    public PerennialSeasonDraft(GrowthPeriodOverrides manual)
    {
        Values = Original = new(manual.Flowering.Match(x => PlanningInputParser.Format(x.Start), () => ""), manual.Flowering.Match(x => PlanningInputParser.Format(x.End), () => ""),
            manual.Fruiting.Match(x => PlanningInputParser.Format(x.Start), () => ""), manual.Fruiting.Match(x => PlanningInputParser.Format(x.End), () => ""));
    }
    public string FlowerStart { get => Values.FlowerStart; set => Update(Values with { FlowerStart = value }); }
    public string FlowerEnd { get => Values.FlowerEnd; set => Update(Values with { FlowerEnd = value }); }
    public string FruitStart { get => Values.FruitStart; set => Update(Values with { FruitStart = value }); }
    public string FruitEnd { get => Values.FruitEnd; set => Update(Values with { FruitEnd = value }); }
    public Result<GrowthPeriodOverrides> Build() => PerennialEditorDraft.OptionalWindow(FlowerStart, FlowerEnd).Bind(flower =>
        PerennialEditorDraft.OptionalWindow(FruitStart, FruitEnd).Bind(fruit => Result.Success(new GrowthPeriodOverrides { Flowering = flower, Fruiting = fruit })));
    public void Accept(PerennialSeasonValues submitted) { Original = submitted; OnPropertyChanged(nameof(Original)); }
    public string Error => PlanningInputParser.Error(Build());
    public string this[string property] => Error;
    private void Update(PerennialSeasonValues values, [CallerMemberName] string? name = null)
    { if (Values == values) return; Values = values; OnPropertyChanged(nameof(Values)); OnPropertyChanged(name); OnPropertyChanged(nameof(Error)); }
}
