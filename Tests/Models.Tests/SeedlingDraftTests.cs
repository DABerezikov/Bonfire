using Bonfire.Models.Mappers;
using BonfireDB.Entities;

namespace Models.Tests;

public class SeedlingDraftTests
{
    [Fact]
    public void CopyInto_EditingNestedDraft_LeavesOriginalUnchanged()
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
        var draft = Batch();

        SeedlingMapper.CopyInto(original, draft);
        var edited = Assert.Single(draft.SeedlingInfos);
        edited.Note = "Правка";
        edited.Replants![0].ReplantingNote = "Правка";
        edited.Treatments![0].Product = "Правка";
        draft.SeedlingInfos.Clear();

        Assert.Single(original.SeedlingInfos);
        Assert.Equal("Исходная запись", info.Note);
        Assert.Equal("Исходная пересадка", info.Replants[0].ReplantingNote);
        Assert.Equal("Исходный препарат", info.Treatments[0].Product);
        Assert.Same(edited, edited.Replants[0].SeedlingInfo);
        Assert.Same(edited, edited.Treatments[0].SeedlingInfo);
    }

    [Fact]
    public void CopyInto_BatchMetadata_PreservesValuesAndOptionalCollections()
    {
        var original = Batch();
        original.LandingDate = new DateTime(2026, 3, 1);
        original.LunarPhase = "Фаза";
        original.SeedlingSource = "Источник";
        original.PlantPlace = "Окно";
        original.PlantedOut = 2;
        original.IsPlantedInBed = true;
        original.SeedlingInfos.Add(new() { Id = 7, MotherPlantId = 3, IsDead = false });
        var draft = Batch();

        SeedlingMapper.CopyInto(original, draft);

        Assert.Equal(original.LandingDate, draft.LandingDate);
        Assert.Equal(original.LunarPhase, draft.LunarPhase);
        Assert.Equal(original.SeedlingSource, draft.SeedlingSource);
        Assert.Equal(original.PlantPlace, draft.PlantPlace);
        Assert.Equal(2, draft.PlantedOut);
        Assert.True(draft.IsPlantedInBed);
        Assert.Null(draft.SeedlingInfos[0].Replants);
        Assert.Null(draft.SeedlingInfos[0].Treatments);
        Assert.Equal(3, draft.SeedlingInfos[0].MotherPlantId);
    }

    private static Seedling Batch() => new()
    {
        Plant = new() { PlantCulture = new(), PlantSort = new() { Producer = new() } }
    };
}
