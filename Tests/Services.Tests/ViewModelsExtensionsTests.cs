namespace Services.Tests;

public class ViewModelsExtensionsTests
{
    // ── SortSeeds ─────────────────────────────────────────────────────────────

    [Fact]
    public void SortSeeds_OrdersByCultureThenSortThenProducer()
    {
        var seeds = new List<SeedsFromViewModel>
        {
            new() { Culture = "Томат", Sort = "Черри", Producer = "Б" },
            new() { Culture = "Огурец", Sort = "Буян", Producer = "А" },
            new() { Culture = "Томат", Sort = "Бычье сердце", Producer = "А" },
        };

        var sorted = seeds.SortSeeds().ToList();

        Assert.Equal("Огурец", sorted[0].Culture);
        Assert.Equal("Томат", sorted[1].Culture);
        Assert.Equal("Томат", sorted[2].Culture);
        Assert.Equal("Бычье сердце", sorted[1].Sort);
        Assert.Equal("Черри", sorted[2].Sort);
    }

    [Fact]
    public void SortSeeds_SameCultureAndSort_OrdersByProducer()
    {
        var seeds = new List<SeedsFromViewModel>
        {
            new() { Culture = "Томат", Sort = "Черри", Producer = "Гавриш" },
            new() { Culture = "Томат", Sort = "Черри", Producer = "Аэлита" },
        };

        var sorted = seeds.SortSeeds().ToList();

        Assert.Equal("Аэлита", sorted[0].Producer);
        Assert.Equal("Гавриш", sorted[1].Producer);
    }

    [Fact]
    public void SortSeeds_EmptyCollection_ReturnsEmpty()
    {
        var result = Enumerable.Empty<SeedsFromViewModel>().SortSeeds();
        Assert.Empty(result);
    }

    [Fact]
    public void SortSeeds_SingleItem_ReturnsSingleItem()
    {
        var seeds = new List<SeedsFromViewModel> { new() { Culture = "Томат" } };
        Assert.Single(seeds.SortSeeds());
    }

    // ── SortSeedlings ─────────────────────────────────────────────────────────

    [Fact]
    public void SortSeedlings_OrdersByLandingDateFirst()
    {
        var older = new DateTime(2025, 2, 1);
        var newer = new DateTime(2025, 3, 1);

        var seedlings = new List<SeedlingFromViewModel>
        {
            new() { LandingData = newer, Culture = "Томат" },
            new() { LandingData = older, Culture = "Огурец" },
        };

        var sorted = seedlings.SortSeedlings().ToList();

        Assert.Equal(older, sorted[0].LandingData);
        Assert.Equal(newer, sorted[1].LandingData);
    }

    [Fact]
    public void SortSeedlings_SameLandingDate_OrdersByCultureThenSort()
    {
        var date = new DateTime(2025, 3, 1);
        var seedlings = new List<SeedlingFromViewModel>
        {
            new() { LandingData = date, Culture = "Томат", Sort = "Черри" },
            new() { LandingData = date, Culture = "Огурец", Sort = "Буян" },
            new() { LandingData = date, Culture = "Огурец", Sort = "Апрельский" },
        };

        var sorted = seedlings.SortSeedlings().ToList();

        Assert.Equal("Огурец", sorted[0].Culture);
        Assert.Equal("Апрельский", sorted[0].Sort);
        Assert.Equal("Огурец", sorted[1].Culture);
        Assert.Equal("Буян", sorted[1].Sort);
        Assert.Equal("Томат", sorted[2].Culture);
    }

    [Fact]
    public void SortSeedlings_EmptyCollection_ReturnsEmpty()
    {
        var result = Enumerable.Empty<SeedlingFromViewModel>().SortSeedlings();
        Assert.Empty(result);
    }
}
