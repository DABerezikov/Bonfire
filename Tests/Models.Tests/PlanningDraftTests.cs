using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Models.Planning;

namespace Models.Tests;

public class PlanningDraftTests
{
    [Fact]
    public void StockMetadata_DoesNotChangeSelectedSortIdentityOrDirtyState()
    {
        var draft = Draft(); draft.AcceptSaved(draft.Values, 1); var choice = draft.Choice!;
        draft.Choice = choice with { HasAvailableSeeds = true };
        Assert.Equal(choice, draft.Choice);
        Assert.Equal(choice.GetHashCode(), draft.Choice.GetHashCode());
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void Draft_InvalidNumber_KeepsTextAndDoesNotSubstituteZero()
    {
        var draft = Draft();
        draft.Profile.GermMinText = "не число";
        Assert.True(draft.BuildInput().IsFailure);
        Assert.Equal("не число", draft.Profile.GermMinText);
        Assert.NotEmpty(draft.Profile[nameof(draft.Profile.GermMinText)]);
    }

    [Fact]
    public void Draft_EmptyInheritedPercentageDiffersFromExplicitZeroLosses()
    {
        var draft = Draft();
        draft.Profile.LossPercentText = "0";
        var input = draft.BuildInput().ValueOrThrow();
        Assert.IsType<Option<GerminationPercentage>.None>(input.GerminationOverride);
        Assert.Equal(0, input.LossOverride.Match(x => x.Value, () => -1));
        Assert.IsType<Option<DayRange>.None>(input.Overrides.Germination);
    }

    [Fact]
    public void Draft_SwitchMethod_KeepsSeparateWindowsAndIgnoresSeedlingFieldsForDirectSow()
    {
        var draft = Draft();
        draft.DirectStartText = "01.06.2027";
        draft.DirectEndText = "05.06.2027";
        draft.Profile.AgeMinText = "не число";
        draft.DirectSow = true;
        Assert.Equal(new DateOnly(2027, 6, 1), Assert.IsType<GrowingMethod.DirectSow>(draft.BuildInput().ValueOrThrow().Method).SowingWindow.Start);
        draft.DirectSow = false;
        Assert.Equal("20.05.2027", draft.TransplantStartText);
        Assert.True(draft.BuildInput().IsFailure);
    }

    [Fact]
    public void Draft_AcceptSaved_PreservesLaterInputAndIndependentOriginal()
    {
        var draft = Draft();
        var submitted = draft.Values;
        draft.TargetPlantsText = "35";
        draft.AcceptSaved(submitted, 12);
        Assert.Equal("35", draft.TargetPlantsText);
        Assert.Equal("20", draft.Original.TargetPlantsText);
        Assert.True(draft.IsDirty);
        Assert.Equal(Option.Some(12), draft.BatchId);
    }

    internal static PlannedBatchEditorDraft Draft()
    {
        var draft = new PlannedBatchEditorDraft(1)
        {
            Choice = new PlanningChoiceFromViewModel(1, "Томат", 2, "Черри"),
            TransplantStartText = "20.05.2027", TransplantEndText = "25.05.2027", TargetPlantsText = "20"
        };
        draft.Profile.ConfigureInheritance(new()
        {
            Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow()), SeedlingAge = Option.Some(DayRange.Create(50, 60).ValueOrThrow())
        }, new(), Option.Some(GerminationPercentage.Create(80).ValueOrThrow()), Option.Some(LossPercentage.Create(0).ValueOrThrow()));
        return draft;
    }
}
