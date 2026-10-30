using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bonfire.Application.Seedlings;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Models;

// Nullable представления существуют только на WPF-границе; исходный снимок неизменяем.
public sealed class SeedlingReadModel(SeedlingReadFromViewModel snapshot)
{
    public SeedlingReadFromViewModel Snapshot { get; } = snapshot;
    public int Id => Snapshot.Id;
    public int SeedId => Snapshot.SeedId;
    public PlantReadFromViewModel Plant => Snapshot.Plant;
    public double Weight => Snapshot.Weight;
    public double Quantity => Snapshot.Quantity;
    public int PlantedOut => Snapshot.PlantedOut;
    public bool IsPlantedInBed => Snapshot.IsPlantedInBed;
    public DateTime? LandingDate => Snapshot.LandingDate.ToNullable();
    public string? LunarPhase => Snapshot.LunarPhase.ToNullable();
    public string? SeedlingSource => Snapshot.SeedlingSource.ToNullable();
    public string? PlantPlace => Snapshot.PlantPlace.ToNullable();
    public IReadOnlyList<SeedlingInfoReadModel> SeedlingInfos { get; } = snapshot.SeedlingInfos.Select(info => new SeedlingInfoReadModel(info)).ToImmutableArray();
    public Result<SeedlingBatch> ToBatch() => SeedlingBatch.Create(Quantity, Snapshot.SeedlingInfos.Select(info => new SeedlingEntry(
        Option.Some(info.Entry.Id), info.Entry.Number, info.Entry.GerminationDate, info.Entry.IsDead)
    {
        DeathNote = info.Entry.DeathNote,
        Replants = info.Entry.Replants.Select(replant => new ReplantingEntry(Option.Some(replant.Id), replant.Date,
            replant.PotVolume, replant.Note)).ToImmutableArray()
    }), PlantedOut);
}

public sealed class SeedlingInfoReadModel(SeedlingInfoReadFromViewModel snapshot)
{
    public SeedlingInfoReadFromViewModel Snapshot { get; } = snapshot;
    public int Id => Snapshot.Entry.Id;
    public int SeedlingNumber => Snapshot.Entry.Number;
    public int? MotherPlantId => Snapshot.MotherPlantId.ToNullable();
    public DateTime LandingDate => Snapshot.LandingDate;
    public string LunarPhase => Snapshot.LunarPhase;
    public string? PlantPlace => Snapshot.PlantPlace.ToNullable();
    public DateTime? GerminationDate => Snapshot.Entry.GerminationDate.ToNullable();
    public DateTime? QuarantineStartDate => Snapshot.QuarantineStartDate.ToNullable();
    public DateTime? QuarantineStopDate => Snapshot.QuarantineStopDate.ToNullable();
    public string? QuarantineCause => Snapshot.QuarantineCause.ToNullable();
    public string? QuarantineNote => Snapshot.QuarantineNote.ToNullable();
    public string SeedlingSource => Snapshot.SeedlingSource;
    public string? Note => Snapshot.Note.ToNullable();
    public DateTime? QuenchingDate => Snapshot.QuenchingDate.ToNullable();
    public string? DeathNote => Snapshot.Entry.DeathNote.ToNullable();
    public bool? IsDead => Snapshot.Entry.IsDead.ToNullable();
    public IReadOnlyList<ReplantingReadModel> Replants { get; } = snapshot.Entry.Replants.Select(entry =>
        new ReplantingReadModel(entry.Id, entry.Date, entry.PotVolume.ToNullable(), entry.Note.ToNullable())).ToImmutableArray();
}

public sealed record ReplantingReadModel(int Id, DateTime ReplantingDate, double? PotVolume, string? ReplantingNote);
