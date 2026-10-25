using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SeedDeletionTests
{
    [Theory]
    [InlineData(0, "Причина")]
    [InlineData(-1, "Причина")]
    [InlineData(1, null)]
    public void Create_InvalidInput_ReturnsFailure(int id, string? reason)
        => Assert.True(SeedDeletion.Create(id, reason!).IsFailure);

    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("  Причина  ", "Причина")]
    public void Create_ValidReason_PreservesEmptyAndTrimsText(string reason, string expected)
    {
        var deletion = SeedDeletion.Create(1, reason).ValueOrThrow();
        Assert.Equal(1, deletion.SeedId.Value);
        Assert.Equal(expected, deletion.Reason);
    }

    [Fact]
    public void WithReason_ValidReason_ReturnsNewIntentWithoutChangingOriginal()
    {
        var original = SeedDeletion.Create(1, "Старое").ValueOrThrow();
        var updated = original.WithReason(" Новое ").ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.Equal(original.SeedId, updated.SeedId);
        Assert.Equal("Старое", original.Reason);
        Assert.Equal("Новое", updated.Reason);
    }
}
