using Bonfire.Infrastructure;

namespace Services.Tests;

public class CollisionHelperTests
{
    // ── Overlaps ──────────────────────────────────────────────────────────────

    [Fact]
    public void Overlaps_TouchingRightBorder_ReturnsFalse()
    {
        // A ends exactly where B begins (right edge touches left edge)
        Assert.False(CollisionHelper.Overlaps(0, 0, 100, 80, 100, 0, 100, 80));
    }

    [Fact]
    public void Overlaps_TouchingBottomBorder_ReturnsFalse()
    {
        // A ends exactly where B begins (bottom edge touches top edge)
        Assert.False(CollisionHelper.Overlaps(0, 0, 100, 80, 0, 80, 100, 80));
    }

    [Fact]
    public void Overlaps_TouchingTopBorder_ReturnsFalse()
    {
        // B ends exactly where A begins (B bottom = A top)
        Assert.False(CollisionHelper.Overlaps(0, 80, 100, 80, 0, 0, 100, 80));
    }

    [Fact]
    public void Overlaps_TouchingLeftBorder_ReturnsFalse()
    {
        // B right edge == A left edge
        Assert.False(CollisionHelper.Overlaps(100, 0, 100, 80, 0, 0, 100, 80));
    }

    [Fact]
    public void Overlaps_OnePixelOverlap_ReturnsTrue()
    {
        // A at (0,0) 100×80; B at (99,0) 100×80 — overlap of 1px horizontally
        Assert.True(CollisionHelper.Overlaps(0, 0, 100, 80, 99, 0, 100, 80));
    }

    [Fact]
    public void Overlaps_OnePixelOverlapVertical_ReturnsTrue()
    {
        // A at (0,0) 100×80; B at (0,79) 100×80 — overlap of 1px vertically
        Assert.True(CollisionHelper.Overlaps(0, 0, 100, 80, 0, 79, 100, 80));
    }

    [Fact]
    public void Overlaps_ClearlyNotOverlapping_ReturnsFalse()
    {
        // Completely separate rectangles
        Assert.False(CollisionHelper.Overlaps(0, 0, 50, 50, 200, 200, 50, 50));
    }

    [Fact]
    public void Overlaps_FullOverlap_ReturnsTrue()
    {
        // Identical rectangles
        Assert.True(CollisionHelper.Overlaps(10, 10, 100, 80, 10, 10, 100, 80));
    }

    [Fact]
    public void Overlaps_BInsideA_ReturnsTrue()
    {
        // B is fully inside A
        Assert.True(CollisionHelper.Overlaps(0, 0, 200, 200, 50, 50, 50, 50));
    }

    [Fact]
    public void Overlaps_AInsideB_ReturnsTrue()
    {
        // A is fully inside B
        Assert.True(CollisionHelper.Overlaps(50, 50, 50, 50, 0, 0, 200, 200));
    }

    // ── CollidesWithAny (elements collection) ─────────────────────────────────

    [Fact]
    public void CollidesWithAny_OverlappingElement_ReturnsTrue()
    {
        var existing = new GardenElementFromViewModel { X = 0, Y = 0, Width = 100, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { existing };

        // New rect at (50,0) overlaps existing
        var result = CollisionHelper.CollidesWithAny(50, 0, 100, 80, elements);

        Assert.True(result);
    }

    [Fact]
    public void CollidesWithAny_NonOverlappingElement_ReturnsFalse()
    {
        var existing = new GardenElementFromViewModel { X = 0, Y = 0, Width = 100, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { existing };

        // New rect at (200,0) — no overlap
        var result = CollisionHelper.CollidesWithAny(200, 0, 100, 80, elements);

        Assert.False(result);
    }

    [Fact]
    public void CollidesWithAny_ExcludedElementOverlaps_ReturnsFalse()
    {
        var target = new GardenElementFromViewModel { X = 0, Y = 0, Width = 100, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { target };

        // The only overlapping element is excluded
        var result = CollisionHelper.CollidesWithAny(0, 0, 100, 80, elements, exclude: target);

        Assert.False(result);
    }

    [Fact]
    public void CollidesWithAny_ExcludedPlusOtherOverlapping_ReturnsTrue()
    {
        var excluded = new GardenElementFromViewModel { X = 0, Y = 0, Width = 100, Height = 80 };
        var other = new GardenElementFromViewModel { X = 10, Y = 10, Width = 100, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { excluded, other };

        // excluded is ignored, but 'other' still overlaps
        var result = CollisionHelper.CollidesWithAny(0, 0, 100, 80, elements, exclude: excluded);

        Assert.True(result);
    }

    [Fact]
    public void CollidesWithAny_EmptyCollection_ReturnsFalse()
    {
        var result = CollisionHelper.CollidesWithAny(
            0, 0, 100, 80,
            new List<GardenElementFromViewModel>());

        Assert.False(result);
    }

    // ── FindFreeSpot ──────────────────────────────────────────────────────────

    [Fact]
    public void FindFreeSpot_EmptyCanvas_ReturnsOrigin()
    {
        var result = CollisionHelper.FindFreeSpot(
            elements: new List<GardenElementFromViewModel>(),
            greenhouses: null,
            exclude: null,
            w: 100, h: 80,
            canvasW: 800, canvasH: 600);

        Assert.NotNull(result);
        Assert.Equal((0.0, 0.0), result!.Value);
    }

    [Fact]
    public void FindFreeSpot_OneElementAtOrigin_PlacesNextToIt()
    {
        var existing = new GardenElementFromViewModel { X = 0, Y = 0, Width = 120, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { existing };

        var result = CollisionHelper.FindFreeSpot(
            elements: elements,
            greenhouses: null,
            exclude: null,
            w: 120, h: 80,
            canvasW: 800, canvasH: 600);

        Assert.NotNull(result);
        var (rx, ry) = result!.Value;
        // The result must not overlap the existing element
        Assert.False(CollisionHelper.Overlaps(rx, ry, 120, 80, existing.X, existing.Y, existing.Width, existing.Height));
        // Must be within canvas bounds
        Assert.True(rx >= 0 && rx + 120 <= 800);
        Assert.True(ry >= 0 && ry + 80 <= 600);
    }

    [Fact]
    public void FindFreeSpot_CanvasFullyBlocked_ReturnsNull()
    {
        // Canvas is 120×80. Fill it entirely with a single element of same size.
        var blocker = new GardenElementFromViewModel { X = 0, Y = 0, Width = 120, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { blocker };

        var result = CollisionHelper.FindFreeSpot(
            elements: elements,
            greenhouses: null,
            exclude: null,
            w: 120, h: 80,
            canvasW: 120, canvasH: 80);

        Assert.Null(result);
    }

    [Fact]
    public void FindFreeSpot_WithGreenhouseBlockingOrigin_ReturnsNonOverlappingSpot()
    {
        var gh = new GreenhouseFromViewModel { X = 0, Y = 0, DisplayWidth = 120, DisplayHeight = 80 };

        var result = CollisionHelper.FindFreeSpot(
            elements: new List<GardenElementFromViewModel>(),
            greenhouses: new List<GreenhouseFromViewModel> { gh },
            exclude: null,
            w: 120, h: 80,
            canvasW: 800, canvasH: 600);

        Assert.NotNull(result);
        var (rx, ry) = result!.Value;
        Assert.False(CollisionHelper.Overlaps(rx, ry, 120, 80, gh.X, gh.Y, gh.DisplayWidth, gh.DisplayHeight));
    }

    [Fact]
    public void FindFreeSpot_ExcludedElementIgnored_ReturnsOrigin()
    {
        // The excluded element sits at origin. Without exclude it would block (0,0).
        // With exclude, (0,0) should be free.
        var self = new GardenElementFromViewModel { X = 0, Y = 0, Width = 120, Height = 80 };
        var elements = new List<GardenElementFromViewModel> { self };

        var result = CollisionHelper.FindFreeSpot(
            elements: elements,
            greenhouses: null,
            exclude: self,
            w: 120, h: 80,
            canvasW: 800, canvasH: 600);

        Assert.NotNull(result);
        Assert.Equal((0.0, 0.0), result!.Value);
    }
}
