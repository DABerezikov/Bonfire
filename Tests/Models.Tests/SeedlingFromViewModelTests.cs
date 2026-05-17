namespace Models.Tests;

public class SeedlingFromViewModelTests
{
    private static SeedlingInfoFromViewModel MakeInfo(DateTime? germinationDate, bool? isDead = null) =>
        new() { GerminationData = germinationDate, IsDead = isDead };

    // ── Weight ────────────────────────────────────────────────────────────────

    [Fact]
    public void Weight_Zero_ReturnsNull()
    {
        var model = new SeedlingFromViewModel { Weight = 0 };
        Assert.Null(model.Weight);
    }

    [Fact]
    public void Weight_NonZero_ReturnsValue()
    {
        var model = new SeedlingFromViewModel { Weight = 3.5 };
        Assert.Equal(3.5, model.Weight);
    }

    [Fact]
    public void Weight_Null_ReturnsNull()
    {
        var model = new SeedlingFromViewModel { Weight = null };
        Assert.Null(model.Weight);
    }

    // ── Quantity ──────────────────────────────────────────────────────────────

    [Fact]
    public void Quantity_Zero_ReturnsNull()
    {
        var model = new SeedlingFromViewModel { Quantity = 0 };
        Assert.Null(model.Quantity);
    }

    [Fact]
    public void Quantity_NonZero_ReturnsValue()
    {
        var model = new SeedlingFromViewModel { Quantity = 10 };
        Assert.Equal(10, model.Quantity);
    }

    // ── CountGerminate ────────────────────────────────────────────────────────

    [Fact]
    public void CountGerminate_NullCollection_ReturnsZero()
    {
        var model = new SeedlingFromViewModel { SeedlingInfos = null };
        Assert.Equal(0, model.CountGerminate);
    }

    [Fact]
    public void CountGerminate_EmptyCollection_ReturnsZero()
    {
        var model = new SeedlingFromViewModel { SeedlingInfos = [] };
        Assert.Equal(0, model.CountGerminate);
    }

    [Fact]
    public void CountGerminate_ThreeItems_ReturnsThree()
    {
        var model = new SeedlingFromViewModel
        {
            SeedlingInfos =
            [
                MakeInfo(DateTime.Today),
                MakeInfo(DateTime.Today.AddDays(1)),
                MakeInfo(DateTime.Today.AddDays(2))
            ]
        };
        Assert.Equal(3, model.CountGerminate);
    }

    // ── MinGerminate ──────────────────────────────────────────────────────────

    [Fact]
    public void MinGerminate_EmptyCollection_ReturnsNull()
    {
        var model = new SeedlingFromViewModel
        {
            LandingData = DateTime.Today,
            SeedlingInfos = []
        };
        Assert.Null(model.MinGerminate);
    }

    [Fact]
    public void MinGerminate_SingleItem_ReturnsDaysDiff()
    {
        var landing = new DateTime(2025, 3, 1);
        var germination = new DateTime(2025, 3, 8);
        var model = new SeedlingFromViewModel
        {
            LandingData = landing,
            SeedlingInfos = [MakeInfo(germination)]
        };
        Assert.Equal(7, model.MinGerminate);
    }

    [Fact]
    public void MinGerminate_MultipleItems_ReturnsMinDays()
    {
        var landing = new DateTime(2025, 3, 1);
        var model = new SeedlingFromViewModel
        {
            LandingData = landing,
            SeedlingInfos =
            [
                MakeInfo(new DateTime(2025, 3, 8)), // 7 days
                MakeInfo(new DateTime(2025, 3, 11)), // 10 days
                MakeInfo(new DateTime(2025, 3, 15))
            ]
        };
        Assert.Equal(7, model.MinGerminate);
    }

    // ── MaxGerminate ──────────────────────────────────────────────────────────

    [Fact]
    public void MaxGerminate_EmptyCollection_ReturnsNull()
    {
        var model = new SeedlingFromViewModel
        {
            LandingData = DateTime.Today,
            SeedlingInfos = []
        };
        Assert.Null(model.MaxGerminate);
    }

    [Fact]
    public void MaxGerminate_MultipleItems_ReturnsMaxDays()
    {
        var landing = new DateTime(2025, 3, 1);
        var model = new SeedlingFromViewModel
        {
            LandingData = landing,
            SeedlingInfos =
            [
                MakeInfo(new DateTime(2025, 3, 8)), // 7 days
                MakeInfo(new DateTime(2025, 3, 11)), // 10 days
                MakeInfo(new DateTime(2025, 3, 15))
            ]
        };
        Assert.Equal(14, model.MaxGerminate);
    }

    // ── Balance ───────────────────────────────────────────────────────────────

    [Fact]
    public void Balance_NullCollection_ReturnsNull()
    {
        var model = new SeedlingFromViewModel { SeedlingInfos = null };
        Assert.Null(model.Balance);
    }

    [Fact]
    public void Balance_NoDeadItems_EqualsCountGerminate()
    {
        var model = new SeedlingFromViewModel
        {
            SeedlingInfos =
            [
                MakeInfo(DateTime.Today, isDead: false),
                MakeInfo(DateTime.Today, isDead: false),
                MakeInfo(DateTime.Today, isDead: false)
            ]
        };
        Assert.Equal(3, model.Balance);
    }

    [Fact]
    public void Balance_SomeDeadItems_SubtractsDeadFromIndex1()
    {
        // Skip(1) — первый элемент не считается мёртвым
        var model = new SeedlingFromViewModel
        {
            SeedlingInfos =
            [
                MakeInfo(DateTime.Today, isDead: true), // index 0 — пропускается Skip(1)
                MakeInfo(DateTime.Today, isDead: true), // index 1 — считается
                MakeInfo(DateTime.Today, isDead: false)
            ]
        };
        // CountGerminate=3, dead from index 1 = 1 → Balance = 3 - 1 = 2
        Assert.Equal(2, model.Balance);
    }

    [Fact]
    public void Balance_AllDeadFromIndex1_ReturnsCountMinusDead()
    {
        var model = new SeedlingFromViewModel
        {
            SeedlingInfos =
            [
                MakeInfo(DateTime.Today, isDead: true), // index 0 — пропускается
                MakeInfo(DateTime.Today, isDead: true), // index 1
                MakeInfo(DateTime.Today, isDead: true)
            ]
        };
        // CountGerminate=3, dead from index 1 = 2 → Balance = 3 - 2 = 1
        Assert.Equal(1, model.Balance);
    }

    [Fact]
    public void Balance_SingleItem_ReturnsOne()
    {
        // Single item: Skip(1) даёт 0 мёртвых → Balance = 1
        var model = new SeedlingFromViewModel
        {
            SeedlingInfos = [MakeInfo(DateTime.Today, isDead: true)]
        };
        Assert.Equal(1, model.Balance);
    }
}
