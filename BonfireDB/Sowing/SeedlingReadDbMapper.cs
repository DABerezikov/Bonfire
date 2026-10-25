using System.Collections.Immutable;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.SeedEditing;

namespace BonfireDB.Sowing;

public static class SeedlingReadDbMapper
{
    public static Result<SeedlingReadFromViewModel> Read(Seedling batch)
        => batch.Plant is null ? Result.Failure<SeedlingReadFromViewModel>($"Рассада {batch.Id}: растение отсутствует")
            : SeedlingBatchDbMapper.Read(batch).Bind(_ => SeedAmount.Create(batch.Weight, SeedUnit.Grams))
                .Bind(_ => SeedReadDbMapper.ReadPlant(batch.Plant))
                .Bind(plant => Result.Success(new SeedlingReadFromViewModel(batch.Id, batch.SeedId, plant, batch.Weight, batch.Quantity,
                    batch.PlantedOut, batch.IsPlantedInBed, batch.LandingDate.ToOption(), batch.LunarPhase.ToOption(),
                    batch.SeedlingSource.ToOption(), batch.PlantPlace.ToOption(), batch.SeedlingInfos.Select(ReadInfo).ToImmutableArray())))
                .Match(Result.Success, error => Result.Failure<SeedlingReadFromViewModel>($"Рассада {batch.Id}: {error}"));

    public static SeedlingInfoReadFromViewModel ReadInfo(SeedlingInfo info)
        => new(SeedlingBatchDbMapper.Receipt(info), info.MotherPlantId.ToOption(), info.LandingDate, info.LunarPhase,
            info.PlantPlace.ToOption(), info.QuarantineStartDate.ToOption(), info.QuarantineStopDate.ToOption(),
            info.QuarantineCause.ToOption(), info.QuarantineNote.ToOption(), info.SeedlingSource, info.Note.ToOption(),
            info.QuenchingDate.ToOption(), (info.Treatments?.Select(t => new TreatmentReadFromViewModel(t.Id,
                t.TreatmentDate, t.Product.ToOption(), t.TreatmentMethod.ToOption())).ToImmutableArray()).ToOption());
}
