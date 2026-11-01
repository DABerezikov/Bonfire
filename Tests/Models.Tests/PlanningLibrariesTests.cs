using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class PlanningLibrariesTests
{
    [Fact]
    public void AnnualWindow_LeapDayAndExplicitYearCrossing_AreNotSilentlyAdjusted()
    {
        var leap = AnnualDateWindow.Create(2, 29, 3, 1, false).ValueOrThrow();
        Assert.True(leap.Project(2027).IsFailure);
        Assert.Equal(new DateOnly(2028, 2, 29), leap.Project(2028).ValueOrThrow().Start);
        Assert.True(AnnualDateWindow.Create(12, 20, 1, 10, false).IsFailure);
        var winter = AnnualDateWindow.Create(12, 20, 1, 10, true).ValueOrThrow();
        Assert.Equal(new DateOnly(2028, 1, 10), winter.Project(2027).ValueOrThrow().End);
        Assert.True(winter.Project(9999).IsFailure);
        Assert.True(leap.Project(0).IsFailure);
        Assert.True(AnnualDateWindow.Create(4, 31, 5, 1, false).IsFailure);
    }

    [Fact]
    public void Region_ValidatesNameAndTimeZone_LeavesClimateUnknown()
    {
        var region = RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow();
        Assert.Equal("Asia/Tomsk", region.TimeZoneId);
        Assert.Equal(TimeSpan.FromHours(7), region.TimeZone.GetUtcOffset(new DateTime(2027, 6, 1)));
        Assert.IsType<Option<AnnualDateWindow>.None>(region.Reference.LastSpringFrost);
        Assert.True(RegionDefinition.Create(" ", "Asia/Tomsk", RegionReference.Empty).IsFailure);
        Assert.True(RegionDefinition.Create("Томск", "made-up-zone", RegionReference.Empty).IsFailure);
    }

    [Fact]
    public void Region_EmptyNoteIsSome_AndTextIsPreserved()
    {
        var details = RegionReference.Empty with { Note = Option.Some(""), Source = Option.Some(" Источник ") };
        var region = RegionDefinition.Create(" Томск ", "Asia/Tomsk", details).ValueOrThrow();
        Assert.Equal(" Томск ", region.Name.Value);
        Assert.Equal(Option.Some(""), region.Reference.Note);
        Assert.Equal(Option.Some(" Источник "), region.Reference.Source);
    }

    [Fact]
    public void ProfileResolver_UsesExistingSortFields_AndPartialSortBlocksCultureFallback()
    {
        var culture = CultureGrowingProfile.Create(CultureId.Create(1).ValueOrThrow(), new GrowingProfileParameters
        {
            Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow()),
            SeedlingAge = Option.Some(DayRange.Create(40, 50).ValueOrThrow())
        }, new PlanningExtras()).ValueOrThrow();
        var sort = new SortCharacteristicsValues
        {
            MinGerminationTime = Option.Some(2), MaxGerminationTime = Option.Some(4)
        };
        var resolved = GrowingProfileResolver.Resolve(culture, sort, new PlanningOverrides(), false).ValueOrThrow();
        Assert.Equal(ParameterSource.Sort, resolved.Germination.Source);
        Assert.Equal(ParameterSource.Culture, resolved.SeedlingAge.Match(x => x.Source, () => throw new Exception()));
        Assert.True(GrowingProfileResolver.Resolve(culture, sort with { MaxGerminationTime = Option.None<int>() },
            new PlanningOverrides(), false).IsFailure);
    }

    [Fact]
    public void ProfileResolver_DirectSowIgnoresSeedlingAgeAndLeavesUnknownVegetationAbsent()
    {
        var culture = CultureGrowingProfile.Create(CultureId.Create(1).ValueOrThrow(), new GrowingProfileParameters
        {
            Germination = Option.Some(DayRange.Create(5, 10).ValueOrThrow())
        }, new PlanningExtras()).ValueOrThrow();
        var resolved = GrowingProfileResolver.Resolve(culture, new SortCharacteristicsValues
        {
            AgeOfSeedlings = Option.Some(20)
        }, new PlanningOverrides(), true).ValueOrThrow();
        Assert.IsType<Option<SourcedValue<DayRange>>.None>(resolved.SeedlingAge);
        Assert.IsType<Option<SourcedValue<DayRange>>.None>(resolved.Vegetation);
        Assert.True(GrowingProfileResolver.Resolve(culture, new SortCharacteristicsValues(), new PlanningOverrides(), false).IsFailure);
    }

    [Fact]
    public void ExtraSettings_SortDisableOverridesCulture_WithoutChangingCulture()
    {
        var pricking = DayRange.Create(10, 15).ValueOrThrow();
        var culture = new PlanningExtras { PrickingOut = new OperationOverride<DayRange>.Enabled(pricking) };
        var sort = new PlanningExtras { PrickingOut = new OperationOverride<DayRange>.Disabled() };
        var result = GrowingProfileResolver.ResolveExtras(culture, sort, new PlanningOverrides());
        Assert.IsType<OperationPlan<DayRange>.Disabled>(result.PrickingOut);
        Assert.IsType<OperationOverride<DayRange>.Enabled>(culture.PrickingOut);
        var batch = GrowingProfileResolver.ResolveExtras(culture, sort, new PlanningOverrides
        {
            PrickingOut = new OperationOverride<DayRange>.Enabled(pricking)
        });
        Assert.Equal(ParameterSource.Batch, Assert.IsType<OperationPlan<DayRange>.Enabled>(batch.PrickingOut).Setting.Source);
    }
}
