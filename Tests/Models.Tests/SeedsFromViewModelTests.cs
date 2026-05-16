namespace Models.Tests;

public class SeedsFromViewModelTests
{
    // ── WeightPack ────────────────────────────────────────────────────────────

    [Fact]
    public void WeightPack_Zero_ReturnsNull()
    {
        var model = new SeedsFromViewModel { WeightPack = 0.0 };
        Assert.Null(model.WeightPack);
    }

    [Fact]
    public void WeightPack_NonZero_ReturnsValue()
    {
        var model = new SeedsFromViewModel { WeightPack = 2.5 };
        Assert.Equal(2.5, model.WeightPack);
    }

    [Fact]
    public void WeightPack_Null_ReturnsNull()
    {
        var model = new SeedsFromViewModel { WeightPack = null };
        Assert.Null(model.WeightPack);
    }

    // ── QuantityPack ──────────────────────────────────────────────────────────

    [Fact]
    public void QuantityPack_Zero_ReturnsNull()
    {
        var model = new SeedsFromViewModel { QuantityPack = 0.0 };
        Assert.Null(model.QuantityPack);
    }

    [Fact]
    public void QuantityPack_NonZero_ReturnsValue()
    {
        var model = new SeedsFromViewModel { QuantityPack = 100.0 };
        Assert.Equal(100.0, model.QuantityPack);
    }

    // ── AmountSeedsWeight ─────────────────────────────────────────────────────

    [Fact]
    public void AmountSeedsWeight_Zero_ReturnsNull()
    {
        var model = new SeedsFromViewModel { AmountSeedsWeight = 0 };
        Assert.Null(model.AmountSeedsWeight);
    }

    [Fact]
    public void AmountSeedsWeight_NonZero_ReturnsValue()
    {
        var model = new SeedsFromViewModel { AmountSeedsWeight = 1.0 };
        Assert.Equal(1.0, model.AmountSeedsWeight);
    }

    // ── AmountSeedsQuantity ───────────────────────────────────────────────────

    [Fact]
    public void AmountSeedsQuantity_Zero_ReturnsNull()
    {
        var model = new SeedsFromViewModel { AmountSeedsQuantity = 0 };
        Assert.Null(model.AmountSeedsQuantity);
    }

    [Fact]
    public void AmountSeedsQuantity_NonZero_ReturnsValue()
    {
        var model = new SeedsFromViewModel { AmountSeedsQuantity = 1.0 };
        Assert.Equal(1.0, model.AmountSeedsQuantity);
    }

    // ── IsStillGood ───────────────────────────────────────────────────────────

    [Fact]
    public void IsStillGood_ExpirationCurrentYear_ReturnsTrue()
    {
        var model = new SeedsFromViewModel { ExpirationDate = new DateTime(DateTime.Now.Year, 6, 1) };
        Assert.True(model.IsStillGood);
    }

    [Fact]
    public void IsStillGood_ExpirationPastYear_ReturnsFalse()
    {
        var model = new SeedsFromViewModel { ExpirationDate = new DateTime(DateTime.Now.Year - 1, 1, 1) };
        Assert.False(model.IsStillGood);
    }

    [Fact]
    public void IsStillGood_ExpirationFutureYear_ReturnsFalse()
    {
        // diff < -1 → ни IsStillGood ни IsOld
        var model = new SeedsFromViewModel { ExpirationDate = new DateTime(DateTime.Now.Year + 2, 1, 1) };
        Assert.False(model.IsStillGood);
    }

    // ── IsOld ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsOld_ExpirationPastYear_ReturnsTrue()
    {
        var model = new SeedsFromViewModel { ExpirationDate = DateTime.Now - TimeSpan.FromDays(400) };
        Assert.True(model.IsOld);
    }

    [Fact]
    public void IsOld_ExpirationCurrentYear_ReturnsFalse()
    {
        var model = new SeedsFromViewModel { ExpirationDate = new DateTime(DateTime.Now.Year, 1, 1) };
        Assert.False(model.IsOld);
    }

    [Fact]
    public void IsOld_ExpirationFutureYear_ReturnsFalse()
    {
        var model = new SeedsFromViewModel { ExpirationDate = new DateTime(DateTime.Now.Year + 1, 1, 1) };
        Assert.False(model.IsOld);
    }
}
