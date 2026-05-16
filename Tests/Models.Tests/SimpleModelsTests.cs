namespace Models.Tests;

public class CultureFromViewModelTests
{
    [Fact]
    public void ToString_ReturnsName()
    {
        var model = new CultureFromViewModel { Name = "Томат" };
        Assert.Equal("Томат", model.ToString());
    }

    [Fact]
    public void ToString_NullName_ReturnsNull()
    {
        var model = new CultureFromViewModel { Name = null };
        Assert.Null(model.ToString());
    }

    [Fact]
    public void ToString_EmptyName_ReturnsEmpty()
    {
        var model = new CultureFromViewModel { Name = "" };
        Assert.Equal("", model.ToString());
    }
}

public class ProducerFromViewModelTests
{
    [Fact]
    public void ToString_ReturnsName()
    {
        var model = new ProducerFromViewModel { Name = "Гавриш" };
        Assert.Equal("Гавриш", model.ToString());
    }

    [Fact]
    public void ToString_NullName_ReturnsNull()
    {
        var model = new ProducerFromViewModel { Name = null };
        Assert.Null(model.ToString());
    }
}

public class SortFromSeedsViewModelTests
{
    [Fact]
    public void ToString_ReturnsName()
    {
        var model = new SortFromSeedsViewModel { Name = "Черри" };
        Assert.Equal("Черри", model.ToString());
    }

    [Fact]
    public void ToString_NullName_ReturnsNull()
    {
        var model = new SortFromSeedsViewModel { Name = null };
        Assert.Null(model.ToString());
    }
}

public class SortFromSeedlingsViewModelTests
{
    [Fact]
    public void ToString_ReturnsSortName()
    {
        var model = new SortFromSeedlingsViewModel { Sort = "Буян", Culture = "Огурец" };
        Assert.Equal("Буян", model.ToString());
    }

    [Fact]
    public void ToString_NullSort_ReturnsNull()
    {
        var model = new SortFromSeedlingsViewModel { Sort = null, Culture = "Огурец" };
        Assert.Null(model.ToString());
    }
}
