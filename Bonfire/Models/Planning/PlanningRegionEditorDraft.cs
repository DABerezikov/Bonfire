using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning.Regions;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record PlanningRegionEditorValues
{
    public string Name { get; init; } = "";
    public string TimeZoneId { get; init; } = "";
    public string SpringStart { get; init; } = "";
    public string SpringEnd { get; init; } = "";
    public string AutumnStart { get; init; } = "";
    public string AutumnEnd { get; init; } = "";
    public bool SpringNextYear { get; init; }
    public bool AutumnNextYear { get; init; }
    public bool IsArchived { get; init; }
    public string? Note { get; init; }
    public string? Source { get; init; }
}

public sealed class PlanningRegionEditorDraft : ViewModel, IDataErrorInfo
{
    public Option<int> Id { get; private set; }
    public PlanningRegionEditorValues Values { get; private set; }
    public PlanningRegionEditorValues Original { get; private set; }
    public bool IsDirty => Values != Original;
    public PlanningRegionEditorDraft(Option<int> id, PlanningRegionEditorValues values) => (Id, Values, Original) = (id, values, values);
    public string Name { get => Values.Name; set => Update(Values with { Name = value }); }
    public string TimeZoneId { get => Values.TimeZoneId; set => Update(Values with { TimeZoneId = value }); }
    public string SpringStart { get => Values.SpringStart; set => Update(Values with { SpringStart = value }); }
    public string SpringEnd { get => Values.SpringEnd; set => Update(Values with { SpringEnd = value }); }
    public string AutumnStart { get => Values.AutumnStart; set => Update(Values with { AutumnStart = value }); }
    public string AutumnEnd { get => Values.AutumnEnd; set => Update(Values with { AutumnEnd = value }); }
    public bool SpringNextYear { get => Values.SpringNextYear; set => Update(Values with { SpringNextYear = value }); }
    public bool AutumnNextYear { get => Values.AutumnNextYear; set => Update(Values with { AutumnNextYear = value }); }
    public bool IsArchived { get => Values.IsArchived; set => Update(Values with { IsArchived = value }); }
    public string? Note { get => Values.Note; set => Update(Values with { Note = value }); }
    public string? Source { get => Values.Source; set => Update(Values with { Source = value }); }

    public Result<SaveRegionFromViewModel> BuildRequest() => PlanningInputParser.Annual(SpringStart, SpringEnd, SpringNextYear)
        .Bind(spring => PlanningInputParser.Annual(AutumnStart, AutumnEnd, AutumnNextYear).Bind(autumn =>
        {
            var reference = new RegionReference { LastSpringFrost = spring, FirstAutumnFrost = autumn, Note = Note.ToOption(), Source = Source.ToOption() };
            return RegionDefinition.Create(Name, TimeZoneId, reference, IsArchived).Bind(_ =>
                Result.Success(new SaveRegionFromViewModel(Id, Name, TimeZoneId, reference, IsArchived)));
        }));
    public void AcceptSaved(PlanningRegionEditorValues submitted, int id) { Original = submitted; Id = Option.Some(id); OnPropertyChanged(nameof(IsDirty)); }
    public string Error => PlanningInputParser.Error(BuildRequest());
    public string this[string property] => property switch
    {
        nameof(SpringStart) or nameof(SpringEnd) => PlanningInputParser.Error(PlanningInputParser.Annual(SpringStart, SpringEnd, SpringNextYear)),
        nameof(AutumnStart) or nameof(AutumnEnd) => PlanningInputParser.Error(PlanningInputParser.Annual(AutumnStart, AutumnEnd, AutumnNextYear)),
        nameof(Name) or nameof(TimeZoneId) => Error,
        _ => ""
    };
    public static PlanningRegionEditorDraft From(RegionFromViewModel region) => new(Option.Some(region.Id.Value), new()
    {
        Name = region.Definition.Name.Value, TimeZoneId = region.Definition.TimeZoneId, IsArchived = region.Definition.IsArchived,
        Note = region.Definition.Reference.Note.ToNullable(), Source = region.Definition.Reference.Source.ToNullable(),
        SpringStart = Bound(region.Definition.Reference.LastSpringFrost, false), SpringEnd = Bound(region.Definition.Reference.LastSpringFrost, true),
        AutumnStart = Bound(region.Definition.Reference.FirstAutumnFrost, false), AutumnEnd = Bound(region.Definition.Reference.FirstAutumnFrost, true),
        SpringNextYear = region.Definition.Reference.LastSpringFrost.Match(x => x.EndInNextYear, () => false),
        AutumnNextYear = region.Definition.Reference.FirstAutumnFrost.Match(x => x.EndInNextYear, () => false)
    });
    private static string Bound(Option<AnnualDateWindow> window, bool end) => window.Match(
        value => $"{(end ? value.EndDay : value.StartDay):00}.{(end ? value.EndMonth : value.StartMonth):00}", () => "");
    private void Update(PlanningRegionEditorValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(string.Empty);
    }
}
