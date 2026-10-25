using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.SpotStates;

namespace Models.Tests;

public class PlantingCellTests
{
    private static PlantingCell Cell(PlantingSpotState state) => new(state, Option.Some(7),
        Option.Some(new DateTime(2026, 5, 1)), Option.Some(new DateTime(2026, 8, 1)), Option.Some("История"));

    [Fact]
    public void Apply_Transition_PreservesMetadataAndOriginal()
    {
        var original = Cell(new PlantedSpotState());
        var changed = original.Apply(new SpotChange.Transition(new HarvestedSpotState(), Option.None<string>())).ValueOrThrow();
        Assert.IsType<HarvestedSpotState>(changed.State);
        Assert.IsType<PlantedSpotState>(original.State);
        Assert.Equal(original.Note, changed.Note);
        Assert.Equal(original.SeedlingInfoId, changed.SeedlingInfoId);
        Assert.Equal(original.PlantedDate, changed.PlantedDate);
        Assert.Equal(original.HarvestDate, changed.HarvestDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(" Томат ")]
    public void Apply_ExplicitLabel_PreservesExactText(string label)
    {
        var changed = Cell(new EmptySpotState()).Apply(new SpotChange.Transition(new ReservedSpotState(), Option.Some(label))).ValueOrThrow();
        Assert.Equal(label, changed.Note.ToNullable());
    }

    [Fact]
    public void Apply_Clear_IsIndependentOfTransitionMatrixAndKeepsHistoricalNote()
    {
        var original = Cell(new PlantedSpotState());
        var changed = original.Apply(new SpotChange.Clear()).ValueOrThrow();
        Assert.IsType<EmptySpotState>(changed.State);
        Assert.Equal(original.Note, changed.Note);
        Assert.IsType<Option<int>.None>(changed.SeedlingInfoId);
        Assert.IsType<Option<DateTime>.None>(changed.PlantedDate);
        Assert.IsType<Option<DateTime>.None>(changed.HarvestDate);
        Assert.IsType<Option<int>.Some>(original.SeedlingInfoId);
    }

    [Fact]
    public void Apply_InvalidTransition_ReturnsExplicitReason()
        => Assert.Contains("запрещён", Cell(new PlantedSpotState()).Apply(new SpotChange.Transition(new ReservedSpotState(), Option.Some("Другое"))).Match(_ => "", error => error));
}
