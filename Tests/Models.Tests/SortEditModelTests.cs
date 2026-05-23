namespace Models.Tests;

public class SortEditModelTests
{
    private static SortEditModel ValidDirty()
    {
        var m = new SortEditModel { Id = 1, Name = "Черри" };
        m.ResetDirty();
        m.Name = "Черри 2";
        return m;
    }

    // ── IsDirty ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsDirty_AfterResetDirty_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X" };
        m.ResetDirty();
        Assert.False(m.IsDirty);
    }

    [Fact]
    public void IsDirty_AfterPropertyChange_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X" };
        m.ResetDirty();
        m.Description = "desc";
        Assert.True(m.IsDirty);
    }

    // ── HasErrors: Name ───────────────────────────────────────────────────────

    [Fact]
    public void HasErrors_EmptyName_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = string.Empty };
        Assert.True(m.HasErrors);
    }

    [Fact]
    public void HasErrors_NullName_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = null };
        Assert.True(m.HasErrors);
    }

    [Fact]
    public void HasErrors_ValidName_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "Черри" };
        Assert.False(m.HasErrors);
    }

    // ── HasErrors: MinGerminationTime ─────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativeMin_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = -1 };
        Assert.True(m.HasErrors);
    }

    [Fact]
    public void HasErrors_ZeroMin_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = 0 };
        Assert.False(m.HasErrors);
    }

    // ── HasErrors: MaxGerminationTime ─────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativeMax_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MaxGerminationTime = -5 };
        Assert.True(m.HasErrors);
    }

    // ── HasErrors: Min > Max ──────────────────────────────────────────────────

    [Fact]
    public void HasErrors_MinGreaterThanMax_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = 20, MaxGerminationTime = 10 };
        Assert.True(m.HasErrors);
    }

    [Fact]
    public void HasErrors_MinEqualMax_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = 10, MaxGerminationTime = 10 };
        Assert.False(m.HasErrors);
    }

    // ── HasErrors: AgeOfSeedlings ─────────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativeAgeOfSeedlings_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", AgeOfSeedlings = -1 };
        Assert.True(m.HasErrors);
    }

    [Fact]
    public void HasErrors_ZeroAgeOfSeedlings_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X", AgeOfSeedlings = 0 };
        Assert.False(m.HasErrors);
    }

    // ── HasErrors: GrowingSeason ──────────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativeGrowingSeason_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", GrowingSeason = -2 };
        Assert.True(m.HasErrors);
    }

    // ── HasErrors: LandingPattern ─────────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativeLandingPattern_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", LandingPattern = -1 };
        Assert.True(m.HasErrors);
    }

    // ── HasErrors: PlantHeight ────────────────────────────────────────────────

    [Fact]
    public void HasErrors_NegativePlantHeight_IsTrue()
    {
        var m = new SortEditModel { Id = 1, Name = "X", PlantHeight = -100 };
        Assert.True(m.HasErrors);
    }

    // ── HasErrors: PlantColor — любая строка допустима ───────────────────────

    [Fact]
    public void HasErrors_ArbitraryPlantColor_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X", PlantColor = "красный" };
        Assert.False(m.HasErrors);
    }

    [Fact]
    public void HasErrors_HexPlantColor_IsFalse()
    {
        var m = new SortEditModel { Id = 1, Name = "X", PlantColor = "#FF0000" };
        Assert.False(m.HasErrors);
    }

    // ── IDataErrorInfo indexer ────────────────────────────────────────────────

    [Fact]
    public void Indexer_EmptyName_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = string.Empty };
        Assert.NotEmpty(m[nameof(SortEditModel.Name)]);
    }

    [Fact]
    public void Indexer_ValidName_ReturnsEmpty()
    {
        var m = new SortEditModel { Id = 1, Name = "X" };
        Assert.Empty(m[nameof(SortEditModel.Name)]);
    }

    [Fact]
    public void Indexer_NegativeMin_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.MinGerminationTime)]);
    }

    [Fact]
    public void Indexer_NegativeMax_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MaxGerminationTime = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.MaxGerminationTime)]);
    }

    [Fact]
    public void Indexer_MinGreaterThanMax_ReturnsErrorOnMin()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = 30, MaxGerminationTime = 10 };
        Assert.NotEmpty(m[nameof(SortEditModel.MinGerminationTime)]);
    }

    [Fact]
    public void Indexer_NegativeAgeOfSeedlings_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", AgeOfSeedlings = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.AgeOfSeedlings)]);
    }

    [Fact]
    public void Indexer_NegativeGrowingSeason_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", GrowingSeason = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.GrowingSeason)]);
    }

    [Fact]
    public void Indexer_NegativeLandingPattern_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", LandingPattern = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.LandingPattern)]);
    }

    [Fact]
    public void Indexer_NegativePlantHeight_ReturnsError()
    {
        var m = new SortEditModel { Id = 1, Name = "X", PlantHeight = -1 };
        Assert.NotEmpty(m[nameof(SortEditModel.PlantHeight)]);
    }

    // ── Кросс-уведомление Min ↔ Max ──────────────────────────────────────────

    [Fact]
    public void MaxGerminationTime_Changed_NotifiesMinGerminationTime()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MinGerminationTime = 10 };
        m.ResetDirty();

        var notified = new List<string?>();
        m.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        m.MaxGerminationTime = 5;

        Assert.Contains(nameof(SortEditModel.MinGerminationTime), notified);
    }

    [Fact]
    public void MinGerminationTime_Changed_NotifiesMaxGerminationTime()
    {
        var m = new SortEditModel { Id = 1, Name = "X", MaxGerminationTime = 20 };
        m.ResetDirty();

        var notified = new List<string?>();
        m.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        m.MinGerminationTime = 25;

        Assert.Contains(nameof(SortEditModel.MaxGerminationTime), notified);
    }
}
