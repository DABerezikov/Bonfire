using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record HarvestJournalValues(
    HarvestSourceFromViewModel? Source,
    PlanningChoiceFromViewModel? Choice,
    RegionFromViewModel? Region,
    string Season,
    string Date,
    string MassKg,
    string? Note);

public sealed class HarvestJournalDraft : ViewModel, IDataErrorInfo
{
    private readonly TimeProvider _time;
    private Option<HarvestIdentity> _identity = Option.None<HarvestIdentity>();

    public HarvestJournalDraft(TimeProvider time)
    {
        _time = time;
        var date = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        Values = Original = new HarvestJournalValues(null, null, null, date.Year.ToString(),
            PlanningInputParser.Format(date), "", null);
    }

    public HarvestJournalValues Values { get; private set; }
    public HarvestJournalValues Original { get; private set; }
    public Option<int> Id { get; private set; } = Option.None<int>();
    public int Version { get; private set; }
    public bool IsNew => Id is Option<int>.None;
    public bool IsDirty => Values != Original;

    public string IdentityText => _identity.Match(x =>
            $"{x.SeasonYear}: {x.CultureName} / {x.SortName}; {x.RegionName}" +
            x.BatchId.Match(id => $"; партия #{id.Value}",
                () => x.PerennialId.Match(id => $"; многолетняя посадка #{id.Value}", () => "; без партии")),
        () => "Новый сбор");

    public HarvestSourceFromViewModel? Source
    {
        get => Values.Source;
        set => Update(Values with { Source = value });
    }

    public PlanningChoiceFromViewModel? Choice
    {
        get => Values.Choice;
        set => Update(Values with { Choice = value });
    }

    public RegionFromViewModel? Region
    {
        get => Values.Region;
        set => Update(Values with { Region = value });
    }

    public string Season
    {
        get => Values.Season;
        set => Update(Values with { Season = value });
    }

    public string Date
    {
        get => Values.Date;
        set => Update(Values with { Date = value });
    }

    public string MassKg
    {
        get => Values.MassKg;
        set => Update(Values with { MassKg = value });
    }

    public string? Note
    {
        get => Values.Note;
        set => Update(Values with { Note = value });
    }

    public string Error => PlanningInputParser.Error(Build());

    public string this[string name] => name switch
    {
        nameof(Date) => PlanningInputParser.Error(PlanningInputParser.Date(Date, "Дата сбора")),
        nameof(MassKg) => PlanningInputParser.Error(PlanningInputParser.Decimal(MassKg, "Масса кг")
            .Bind(x => x.Match(HarvestMass.Create, () => Result.Failure<HarvestMass>("Введите массу")))),
        _ => ""
    };

    public static HarvestJournalDraft Open(HarvestEntryFromViewModel stored, TimeProvider time)
    {
        var draft = new HarvestJournalDraft(time)
            { Id = Option.Some(stored.Id), Version = stored.Version, _identity = Option.Some(stored.Entry.Identity) };
        draft.Values = draft.Original = draft.Values with
        {
            Season = stored.Entry.Identity.SeasonYear.ToString(), Date = PlanningInputParser.Format(stored.Entry.Date),
            MassKg = stored.Entry.Mass.Kg.ToString(), Note = stored.Entry.Note.ToNullable()
        };
        return draft;
    }

    public Result<SaveHarvestFromViewModel> Build()
    {
        if (_identity is Option<HarvestIdentity>.None && Source is { } selected &&
            (!int.TryParse(Season, out var season) || season != selected.Identity.SeasonYear))
            return Result.Failure<SaveHarvestFromViewModel>(
                "Выбранная партия относится к другому сезону; обновите список партий");
        var identity = _identity.Match(Result.Success,
            () => Source is { } source ? Result.Success(source.Identity) : ManualIdentity());
        return identity.Bind(owner => PlanningInputParser.Date(Date, "Дата сбора").Bind(date => PlanningInputParser
            .Decimal(MassKg, "Масса кг")
            .Bind(mass => mass.Match(HarvestMass.Create, () => Result.Failure<HarvestMass>("Введите массу в кг")))
            .Bind(mass => HarvestEntry.Create(owner, date, mass, Note.ToOption(), DateOnly.FromDateTime(TimeZoneInfo
                .ConvertTime(_time.GetUtcNow(),
                    TimeZoneInfo.FindSystemTimeZoneById(owner.TimeZoneId)).DateTime)))
            .Bind(entry =>
                Result.Success(new SaveHarvestFromViewModel(Id, Version, owner, entry.Date, entry.Mass.Kg,
                    entry.Note)))));
    }

    private Result<HarvestIdentity> ManualIdentity()
    {
        if (Choice is not { } choice || Region is not { } region || !int.TryParse(Season, out var year))
            return Result.Failure<HarvestIdentity>("Выберите партию либо культуру/сорт, регион и сезон");
        return CultureId.Create(choice.CultureId).Bind(c => SortId.Create(choice.SortId).Bind(s =>
            HarvestIdentity.Create(c, s,
                choice.CultureName, choice.SortName, year, region.Definition.Name.Value, region.Definition.TimeZoneId,
                Option.None<PlannedBatchId>(), Option.None<PerennialPlantingId>())));
    }

    public void Accept(HarvestJournalValues submitted, HarvestEntryFromViewModel saved)
    {
        Original = submitted;
        Id = Option.Some(saved.Id);
        Version = saved.Version;
        _identity = Option.Some(saved.Entry.Identity);
        OnPropertyChanged(string.Empty);
    }

    private void Update(HarvestJournalValues values, [CallerMemberName] string? name = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(name);
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Error));
        CommandManager.InvalidateRequerySuggested();
    }
}