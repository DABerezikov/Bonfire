using Bonfire.Models;
using Bonfire.Domain.Functional;
using BonfireDB.Sowing;
using Bonfire.Models.Mappers;
using BonfireDB.Entities;

namespace Models.Tests;

public class SeedlingDraftTests
{
    [Fact]
    public void ReadAndDraft_KeepNestedHistoryIndependent()
    {
        var info = new SeedlingInfo
        {
            Id = 4, Note = "Исходная запись",
            Replants = [new() { Id = 5, ReplantingNote = "Исходная пересадка" }],
            Treatments = [new() { Id = 6, Product = "Исходный препарат" }]
        };
        info.Replants[0].SeedlingInfo = info;
        info.Treatments[0].SeedlingInfo = info;
        var original = Batch();
        original.SeedlingInfos.Add(info);
        var snapshot = new SeedlingReadModel(SeedlingReadDbMapper.Read(original).ValueOrThrow());
        var draft = SeedlingEditMapper.Create(snapshot);
        draft.Note = "Правка";
        info.Replants[0].ReplantingNote = "Внешняя правка";
        info.Treatments[0].Product = "Внешняя правка";
        Assert.Equal("Исходная запись", snapshot.SeedlingInfos[0].Note);
        Assert.Equal("Исходная пересадка", snapshot.SeedlingInfos[0].Replants[0].ReplantingNote);
        Assert.Equal("Исходный препарат", snapshot.Snapshot.SeedlingInfos[0].Treatments.Match(v => v[0].Product.ToNullable(), () => null));
        Assert.Equal("Исходная запись", info.Note);
    }

    [Fact]
    public void Read_BatchMetadata_PreservesValuesAndOptionalCollections()
    {
        var original = Batch();
        original.LandingDate = new DateTime(2026, 3, 1);
        original.LunarPhase = "Фаза";
        original.SeedlingSource = "Источник";
        original.PlantPlace = "Окно";
        original.PlantedOut = 2;
        original.IsPlantedInBed = true;
        original.SeedlingInfos.Add(new() { Id = 7, MotherPlantId = 3, IsDead = false });
        var draft = new SeedlingReadModel(SeedlingReadDbMapper.Read(original).ValueOrThrow());

        Assert.Equal(original.LandingDate, draft.LandingDate);
        Assert.Equal(original.LunarPhase, draft.LunarPhase);
        Assert.Equal(original.SeedlingSource, draft.SeedlingSource);
        Assert.Equal(original.PlantPlace, draft.PlantPlace);
        Assert.Equal(2, draft.PlantedOut);
        Assert.True(draft.IsPlantedInBed);
        Assert.Empty(draft.SeedlingInfos[0].Replants);
        Assert.IsType<Option<System.Collections.Immutable.ImmutableArray<Bonfire.Application.Seedlings.TreatmentReadFromViewModel>>.None>(draft.Snapshot.SeedlingInfos[0].Treatments);
        Assert.Equal(3, draft.SeedlingInfos[0].MotherPlantId);
    }

    private static Seedling Batch() => new()
    {
        Plant = new() { PlantCulture = new(), PlantSort = new() { Producer = new() } }
    };
}
