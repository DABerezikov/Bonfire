using Bonfire.Infrastructure;

namespace Models.Tests;

public class EditingContractTests
{
    [Theory]
    [InlineData(null, "Овощи", true)]
    [InlineData("", "Овощи", true)]
    [InlineData("Томат", "", true)]
    [InlineData("Томат", "Овощи", false)]
    public void CultureEditing_RequiresNameAndClass(string? name, string plantClass, bool invalid)
    {
        var model = new CultureEditModel { Name = name, Class = plantClass };
        Assert.Equal(invalid, model.HasErrors);
        if (string.IsNullOrWhiteSpace(name)) Assert.NotEmpty(model[nameof(model.Name)]);
        if (string.IsNullOrWhiteSpace(plantClass)) Assert.NotEmpty(model[nameof(model.Class)]);
        Assert.Equal(string.Empty, model.Error);
    }

    [Fact]
    public void CultureEditing_ChangesNotifyValidationAndDirtyState()
    {
        var model = new CultureEditModel { Name = "Томат", Class = "Овощи" };
        model.ResetDirty();
        var changes = new System.Collections.Generic.List<string?>();
        model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        model.Class = "Цветы";
        Assert.True(model.IsDirty);
        Assert.Contains(nameof(model.HasErrors), changes);
        model.ResetDirty(); Assert.False(model.IsDirty);
    }

    [Theory]
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 10)]
    [InlineData(10, 0.8, 15.6)]
    [InlineData(10, 0.5, 20)]
    [InlineData(9, 0.8, 14.3)]
    public void AdaptiveFont_CompensatesZoomOutAndCapsGrowth(double size, double zoom, double expected)
        => Assert.Equal(expected, CanvasConstants.AdaptiveFont(size, zoom));
}
