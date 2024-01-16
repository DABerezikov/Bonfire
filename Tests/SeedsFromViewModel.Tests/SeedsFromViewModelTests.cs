namespace SeedsFromViewModel.Tests
{
    public class SeedsFromViewModelTests
    {
        [Fact]
        public void AmountSeedsWeight_ReturnNull()
        {
            //Arrange

            const int expected = 0;

            //Act

            var actual = new Bonfire.Models.SeedsFromViewModel()
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

            var actual = new Bonfire.Models.SeedsFromViewModel()
            {
                AmountSeedsWeight = expected

            };

            //Assert

            Assert.Equal(expected, actual.AmountSeedsWeight);
        }

        [Fact]
        public void AmountSeedsQuantity_ReturnNull()
        {
            //Arrange

            const int expected = 0;

            //Act

            var actual = new Bonfire.Models.SeedsFromViewModel()
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

            var actual = new Bonfire.Models.SeedsFromViewModel()
            {
                AmountSeedsQuantity = expected

            };

            //Assert

            Assert.Equal(expected, actual.AmountSeedsQuantity);
        }

    }
}