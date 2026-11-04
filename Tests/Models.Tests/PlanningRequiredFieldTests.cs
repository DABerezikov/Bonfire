using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Models.Planning;

namespace Models.Tests;

public class PlanningRequiredFieldTests
{
    [Fact]
    public void EnabledOperationsAndLifeCycle_MarkAndValidateTheirConditionalFields()
    {
        var draft = PlanningDraftTests.Draft();
        draft.Profile.PrickingMode = 2;
        Assert.True(draft.Profile.PrickingRangeRule.IsRequired);
        Assert.True(draft.Profile.PrickingRangeRule.Validation.IsFailure);
        Assert.True(draft.BuildInput().IsFailure);
        draft.Profile.PrickingMode = 1;
        Assert.False(draft.Profile.PrickingRangeRule.IsRequired);
        Assert.True(draft.BuildInput().IsSuccess);
        var cycle = draft.Profile.LifeCycle;
        cycle.Mode = 2; cycle.FirstFlower = "";
        Assert.True(cycle.FieldRules[nameof(cycle.FirstFlower)].IsRequired);
        Assert.True(cycle.FieldRules[nameof(cycle.FirstFlower)].Validation.IsFailure);
        Assert.True(draft.BuildInput().IsFailure);
        cycle.Mode = -1;
        Assert.False(cycle.FieldRules[nameof(cycle.FirstFlower)].IsRequired);
        Assert.True(draft.BuildInput().IsSuccess);
    }

    [Theory]
    [InlineData(nameof(PlannedBatchEditorDraft.Choice))]
    [InlineData(nameof(PlannedBatchEditorDraft.Location))]
    [InlineData(nameof(PlannedBatchEditorDraft.Placement))]
    [InlineData(nameof(PlannedBatchEditorDraft.TargetPlantsText))]
    [InlineData(nameof(PlannedBatchEditorDraft.TransplantStartText))]
    [InlineData(nameof(PlannedBatchEditorDraft.TransplantEndText))]
    public void RequiredDraftField_UsesSameRuleForCaptionErrorAndCalculation(string property)
    {
        var draft = PlanningDraftTests.Draft();
        switch (property)
        {
            case nameof(draft.Choice): draft.Choice = null; break;
            case nameof(draft.Location): draft.Location = (GrowingLocation)99; break;
            case nameof(draft.Placement): draft.Placement = (PlantPlacement)99; break;
            default: typeof(PlannedBatchEditorDraft).GetProperty(property)!.SetValue(draft, ""); break;
        }
        var rule = draft.FieldRules[property];
        Assert.True(rule.IsRequired); Assert.EndsWith(" *", rule.Caption);
        Assert.True(rule.Validation.IsFailure); Assert.Equal(rule.Error, draft[property]);
        Assert.True(draft.BuildInput().IsFailure);
    }

    [Fact]
    public void AutomaticSeedFields_AreRequiredUntilManualCountAndRespectInheritedZeroLoss()
    {
        var draft = PlanningDraftTests.Draft();
        draft.Profile.ConfigureInheritance(new() { Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow()),
            SeedlingAge = Option.Some(DayRange.Create(50, 60).ValueOrThrow()) }, new(),
            Option.None<GerminationPercentage>(), Option.None<LossPercentage>());
        Assert.True(draft.Profile.GerminationPercentRule.IsRequired);
        Assert.True(draft.Profile.LossPercentRule.IsRequired);
        Assert.EndsWith(" *", draft.Profile.GerminationPercentLabel);
        Assert.Equal(draft.Profile.GerminationPercentRule.Error, draft.Profile[nameof(draft.Profile.GermPercentText)]);
        Assert.True(draft.BuildInput().IsFailure);
        draft.ManualSeedCountText = "30";
        Assert.False(draft.Profile.GerminationPercentRule.IsRequired);
        Assert.DoesNotContain("*", draft.Profile.GerminationPercentLabel);
        Assert.True(draft.BuildInput().IsSuccess);
        draft.ManualSeedCountText = "";
        draft.Profile.GermPercentText = "80"; draft.Profile.LossPercentText = "0";
        Assert.True(draft.BuildInput().IsSuccess);
    }

    [Theory]
    [InlineData(nameof(HarvestPlanningFields.Length))]
    [InlineData(nameof(HarvestPlanningFields.Width))]
    [InlineData(nameof(HarvestPlanningFields.PlantStep))]
    [InlineData(nameof(HarvestPlanningFields.RowStep))]
    [InlineData(nameof(HarvestPlanningFields.StartMargin))]
    [InlineData(nameof(HarvestPlanningFields.EndMargin))]
    [InlineData(nameof(HarvestPlanningFields.LeftMargin))]
    [InlineData(nameof(HarvestPlanningFields.RightMargin))]
    public void LayoutFields_ShareRequirednessCaptionAndValidation(string property)
    {
        var fields = new HarvestPlanningFields(new()); fields.ApplyBed(1, "Грядка", 2m, .9m);
        typeof(HarvestPlanningFields).GetProperty(property)!.SetValue(fields, "");
        var rule = fields.FieldRules[property];
        Assert.True(rule.IsRequired); Assert.EndsWith(" *", rule.Caption);
        Assert.True(rule.Validation.IsFailure); Assert.Equal(rule.Error, fields[property]);
        Assert.True(fields.Build().IsFailure);
        fields.UseLayout = false;
        Assert.False(fields.FieldRules[property].IsRequired);
        Assert.True(fields.Build().IsSuccess);
    }

    [Theory]
    [InlineData(HarvestPlanningMode.TargetPerPlant, nameof(HarvestPlanningFields.PerPlantKg))]
    [InlineData(HarvestPlanningMode.TargetPerM2, nameof(HarvestPlanningFields.PerM2Kg))]
    public void HarvestTarget_RequiresMatchingRateOrInheritedValue(HarvestPlanningMode mode, string property)
    {
        var fields = new HarvestPlanningFields(new()); fields.ApplyBed(1, "Грядка", 2m, .9m);
        fields.Mode = mode; fields.TargetKg = "10";
        Assert.True(fields.FieldRules[property].IsRequired);
        Assert.True(fields.FieldRules[property].Validation.IsFailure);
        Assert.True(fields.Build().IsFailure);
        fields.ConfigureInheritance(Option.Some(1m), Option.Some(2m));
        Assert.True(fields.FieldRules[property].Validation.IsSuccess);
        Assert.True(fields.Build().IsSuccess);
        fields.TargetKg = "";
        Assert.True(fields.FieldRules[nameof(fields.TargetKg)].IsRequired);
        Assert.True(fields.Build().IsFailure);
    }
}
