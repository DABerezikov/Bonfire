namespace SeedsFromViewModel.Tests;

public class SeedsFromViewModelTests
{
    [Fact]
    public void AmountSeedsWeight_WithZero_ReturnNull()
    {
        //Arrange

        const int expected = 0;

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            AmountSeedsWeight = expected
        };

        //Assert

        Assert.Null(actual.AmountSeedsWeight);
    }

    [Fact]
    public void AmountSeedsWeight_ReturnValue()
    {
        //Arrange

        const double expected = 1.0;

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            AmountSeedsWeight = expected
        };

        //Assert

        Assert.Equal(expected, actual.AmountSeedsWeight);
    }

    [Fact]
    public void AmountSeedsQuantity_WithZero_ReturnNull()
    {
        //Arrange

        const int expected = 0;

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            AmountSeedsQuantity = expected
        };

        //Assert

        Assert.Null(actual.AmountSeedsQuantity);
    }

    [Fact]
    public void AmountSeedsQuantity_ReturnValue()
    {
        //Arrange

        const double expected = 1.0;

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            AmountSeedsQuantity = expected
        };

        //Assert

        Assert.Equal(expected, actual.AmountSeedsQuantity);
    }

    [Fact]
    public void IsStillGood_WithExpirationDateIsCurrentYear_ReturnTrue()
    {
        //Arrange (31.12 is a false)

        var expected = DateTime.Now + TimeSpan.FromDays(1);

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            ExpirationDate = expected
        };

        //Assert

        Assert.True(actual.IsStillGood);
    }

    [Fact]
    public void IsStillGood_WithExpirationDateIsLastYear_ReturnTrue()
    {
        //Arrange

        var expected = DateTime.Now - TimeSpan.FromDays(400);

        //Act

        var actual = new Bonfire.Models.SeedsFromViewModel
        {
            ExpirationDate = expected
        };

        //Assert

        Assert.True(actual.IsOld);
    }
}