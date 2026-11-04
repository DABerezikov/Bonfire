using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;

namespace Bonfire.Application.Seedlings;

public sealed record TreatmentReadFromViewModel(
    int Id,
    DateTime TreatmentDate,
    Option<string> Product,
    Option<string> TreatmentMethod);

public sealed record SeedlingInfoReadFromViewModel(
    SeedlingEntryFromViewModel Entry,
    Option<int> MotherPlantId,
    DateTime LandingDate,
    string LunarPhase,
    Option<string> PlantPlace,
    Option<DateTime> QuarantineStartDate,
    Option<DateTime> QuarantineStopDate,
    Option<string> QuarantineCause,
    Option<string> QuarantineNote,
    string SeedlingSource,
    Option<string> Note,
    Option<DateTime> QuenchingDate,
    Option<ImmutableArray<TreatmentReadFromViewModel>> Treatments);

public sealed record SeedlingReadFromViewModel(
    int Id,
    int SeedId,
    PlantReadFromViewModel Plant,
    double Weight,
    double Quantity,
    int PlantedOut,
    bool IsPlantedInBed,
    Option<DateTime> LandingDate,
    Option<string> LunarPhase,
    Option<string> SeedlingSource,
    Option<string> PlantPlace,
    ImmutableArray<SeedlingInfoReadFromViewModel> SeedlingInfos);

public interface ISeedlingReader
{
    Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
    Task<Result<Option<SeedlingReadFromViewModel>>> ReadAsync(int id, CancellationToken cancellationToken);
}