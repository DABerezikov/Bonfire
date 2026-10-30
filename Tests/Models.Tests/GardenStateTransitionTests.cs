using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;

namespace Models.Tests;

public class GardenStateTransitionTests
{
    private static readonly string[] ElementNames = ["PlannedState", "PreparedState", "ActiveState", "FallowState", "RestingState", "ArchivedState"];
    private static readonly string[] SpotNames = ["EmptySpotState", "ReservedSpotState", "PlantedSpotState", "HarvestedSpotState", "DeadSpotState"];

    public static IEnumerable<object[]> ElementTransitions() =>
        ElementNames.SelectMany(source => ElementNames.Select(target => new object[] { source, target, (source, target) switch
        {
            ("PlannedState", "PreparedState" or "ArchivedState") => true,
            ("PreparedState", "ActiveState" or "FallowState" or "ArchivedState") => true,
            ("ActiveState", "FallowState" or "RestingState") => true,
            ("FallowState", "PreparedState" or "RestingState" or "ArchivedState") => true,
            ("RestingState", "PlannedState" or "PreparedState") => true,
            ("ArchivedState", "PlannedState") => true,
            _ => false
        } }));

    public static IEnumerable<object[]> SpotTransitions() =>
        SpotNames.SelectMany(source => SpotNames.Select(target => new object[] { source, target, (source, target) switch
        {
            ("EmptySpotState", "ReservedSpotState" or "PlantedSpotState") => true,
            ("ReservedSpotState", "PlantedSpotState" or "EmptySpotState") => true,
            ("PlantedSpotState", "HarvestedSpotState" or "DeadSpotState") => true,
            ("HarvestedSpotState", "EmptySpotState" or "PlantedSpotState") => true,
            ("DeadSpotState", "EmptySpotState") => true,
            _ => false
        } }));

    [Theory]
    [MemberData(nameof(ElementTransitions))]
    public void ElementTransition_ExhaustiveMatrix_PreservesOriginalAndReturnsExplicitResult(string source, string target, bool allowed)
    {
        var original = GardenElementState.From(source);
        var destination = GardenElementState.From(target);
        var result = original.TransitionTo(destination);
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed, original.CanTransitionTo(destination));
        Assert.Equal(source, original.GetType().Name);
        if (allowed) Assert.Same(destination, result.ValueOrThrow());
        else Assert.Contains("запрещён", result.Match(_ => "", error => error));
    }

    [Theory]
    [MemberData(nameof(SpotTransitions))]
    public void SpotTransition_ExhaustiveMatrix_PreservesOriginalAndReturnsExplicitResult(string source, string target, bool allowed)
    {
        var original = PlantingSpotState.From(source);
        var destination = PlantingSpotState.From(target);
        var result = original.TransitionTo(destination);
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed, original.CanTransitionTo(destination));
        Assert.Equal(source, original.GetType().Name);
        if (allowed) Assert.Same(destination, result.ValueOrThrow());
        else Assert.Contains("запрещён", result.Match(_ => "", error => error));
    }

    [Fact]
    public void Transitions_NullTarget_RejectInvariantViolation()
    {
        Assert.Throws<ArgumentNullException>(() => new PlannedState().TransitionTo(null!));
        Assert.Throws<ArgumentNullException>(() => new EmptySpotState().TransitionTo(null!));
    }
}
