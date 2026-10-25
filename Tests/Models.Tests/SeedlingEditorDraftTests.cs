using Bonfire.Models.Mappers;
using BonfireDB.Entities;

namespace Models.Tests;

public class SeedlingEditorDraftTests
{
    [Fact]
    public void Draft_EditRowsAndDetails_DoesNotChangeEntityGraphOrEarlierRequest()
    {
        var source = new Seedling { Id = 7, Quantity = 3,
            SeedlingInfos = [new() { Id = 1, Note = "Исходная", LandingDate = new(2026, 3, 1), SeedlingSource = "Из семян" }] };
        var draft = SeedlingEditMapper.Create(source);
        var request = SeedlingEditMapper.ToRequest(draft);
        draft.Quantity = 5;
        draft.Note = "Новая";
        draft.Entries[0].Number = 2;
        draft.Entries[0].DeathNote = "Причина";
        Assert.Equal(3, source.Quantity);
        Assert.Equal("Исходная", source.SeedlingInfos[0].Note);
        Assert.Equal(0, source.SeedlingInfos[0].SeedlingNumber);
        Assert.Null(source.SeedlingInfos[0].DeathNote);
        Assert.Equal(3, request.Details.Quantity);
        Assert.Equal(0, request.Entries[0].Number);
        Assert.Equal(new DateTime(2026, 3, 1), draft.LandingDate);
    }

    [Fact]
    public void Draft_ExistingRootMetadata_IsPreferredAndOptionalValuesStayAbsent()
    {
        var source = new Seedling { Id = 7, LandingDate = new(2026, 4, 1), SeedlingSource = "Подарено",
            SeedlingInfos = [new() { Id = 1, LandingDate = new(2026, 3, 1), SeedlingSource = "Из семян" }] };
        var draft = SeedlingEditMapper.Create(source);
        Assert.Equal(source.LandingDate, draft.LandingDate);
        Assert.Equal("Подарено", draft.Source);
        Assert.Null(draft.Place);
        Assert.Null(draft.Note);
    }
}
