namespace Models.Tests;

public class PlantFromViewModelTests
{
    [Fact]
    public void ToString_ReturnsProducerAndYear()
    {
        var model = new PlantFromViewModel
        {
            Producer = "Гавриш",
            ExpirationDate = new DateTime(2025, 1, 1)
        };
        Assert.Equal("Гавриш 2025", model.ToString());
    }

    [Fact]
    public void ToString_NullProducer_ReturnsSpaceAndYear()
    {
        var model = new PlantFromViewModel
        {
            Producer = null,
            ExpirationDate = new DateTime(2024, 6, 15)
        };
        Assert.Equal(" 2024", model.ToString());
    }

    [Fact]
    public void ToString_EmptyProducer_ReturnsSpaceAndYear()
    {
        var model = new PlantFromViewModel
        {
            Producer = "",
            ExpirationDate = new DateTime(2026, 3, 1)
        };
        Assert.Equal(" 2026", model.ToString());
    }
}
