using Bonfire.Models;

namespace SeedsFromViewModelTests
{
    public class SeedsFromViewModelTests
    {
        [Fact]
        public void AmountSeedsWeight_ReturnNull()
        {
            //Arrange

            var amount = 0;

            //Act

            var actual = new SeedsFromViewModel()
            {
                AmountSeedsWeight = amount

            };

            //Assert

            Assert.Null(actual.AmountSeedsWeight);
        }

        [Fact]
        public void AmountSeedsWeight_ReturnValue()
        {
            //Arrange

            var amount = 1.0;

            //Act

            var actual = new SeedsFromViewModel()
            {
                AmountSeedsWeight = amount

            };

            //Assert

            Assert.Equal(amount, actual.AmountSeedsWeight);
        }

        [Fact]
        public void AmountSeedsQuantity_ReturnNull()
        {
            //Arrange

            var amount = 0;

            //Act

            var actual = new SeedsFromViewModel()
            {
                AmountSeedsQuantity = amount

            };

            //Assert

            Assert.Null(actual.AmountSeedsQuantity);
        }

        [Fact]
        public void AmountSeedsQuantity_ReturnValue()
        {
            //Arrange

            var amount = 1.0;

            //Act

            var actual = new SeedsFromViewModel()
            {
                AmountSeedsQuantity = amount

            };

            //Assert

            Assert.Equal(amount, actual.AmountSeedsQuantity);
        }

    }
}