namespace Services.Tests;

public class SeedsServiceTests
{
    private readonly IRepository<Plant> _plants = Substitute.For<IRepository<Plant>>();
    private readonly IRepository<Seed> _seeds = Substitute.For<IRepository<Seed>>();
    private readonly IRepository<PlantSort> _sort = Substitute.For<IRepository<PlantSort>>();
    private readonly IRepository<PlantCulture> _culture = Substitute.For<IRepository<PlantCulture>>();
    private readonly IRepository<Producer> _producer = Substitute.For<IRepository<Producer>>();
    private readonly IRepository<SeedsInfo> _info = Substitute.For<IRepository<SeedsInfo>>();

    private SeedsService CreateService() =>
        new(_plants, _seeds, _sort, _culture, _producer, _info);

    // ── MakeASeed — plant.Id == 0 (новый) ────────────────────────────────────

    [Fact]
    public async Task MakeASeed_NewCulture_AddsCulture()
    {
        var culture = new PlantCulture { Id = 0, Name = "Томат" };
        var addedCulture = new PlantCulture { Id = 1, Name = "Томат" };
        _culture.AddAsync(culture).Returns(addedCulture);

        var sort = new PlantSort { Id = 1, Producer = new Producer { Id = 1 } };
        var plant = new Plant { Id = 0, PlantCulture = culture, PlantSort = sort };
        var seedsInfo = new SeedsInfo { Id = 1 };
        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = seedsInfo };
        returnedSeed.SeedsInfo ??= seedsInfo;
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);
        _plants.AddAsync(Arg.Any<Plant>()).Returns(plant);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _culture.Received(1).AddAsync(culture);
    }

    [Fact]
    public async Task MakeASeed_ExistingCulture_SkipsCultureAdd()
    {
        var culture = new PlantCulture { Id = 5, Name = "Огурец" };
        var sort = new PlantSort { Id = 3, Producer = new Producer { Id = 2 } };
        var plant = new Plant { Id = 0, PlantCulture = culture, PlantSort = sort };
        var seedsInfo = new SeedsInfo { Id = 1 };
        _plants.AddAsync(Arg.Any<Plant>()).Returns(plant);
        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = seedsInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _culture.DidNotReceive().AddAsync(Arg.Any<PlantCulture>());
    }

    [Fact]
    public async Task MakeASeed_NewProducer_AddsProducer()
    {
        var producer = new Producer { Id = 0, Name = "Гавриш" };
        var addedProducer = new Producer { Id = 1, Name = "Гавриш" };
        _producer.AddAsync(producer).Returns(addedProducer);

        var sort = new PlantSort { Id = 0, Producer = producer };
        var addedSort = new PlantSort { Id = 1, Producer = addedProducer };
        _sort.AddAsync(Arg.Any<PlantSort>()).Returns(addedSort);

        var culture = new PlantCulture { Id = 1 };
        var plant = new Plant { Id = 0, PlantCulture = culture, PlantSort = sort };
        _plants.AddAsync(Arg.Any<Plant>()).Returns(plant);

        var seedsInfo = new SeedsInfo { Id = 1 };
        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = seedsInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _producer.Received(1).AddAsync(producer);
    }

    [Fact]
    public async Task MakeASeed_NewSort_AddsSort()
    {
        var producer = new Producer { Id = 1 };
        var sort = new PlantSort { Id = 0, Producer = producer };
        var addedSort = new PlantSort { Id = 1, Producer = producer };
        _sort.AddAsync(sort).Returns(addedSort);

        var culture = new PlantCulture { Id = 1 };
        var plant = new Plant { Id = 0, PlantCulture = culture, PlantSort = sort };
        _plants.AddAsync(Arg.Any<Plant>()).Returns(plant);

        var seedsInfo = new SeedsInfo { Id = 1 };
        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = seedsInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _sort.Received(1).AddAsync(sort);
    }

    [Fact]
    public async Task MakeASeed_NewPlant_AddsPlant()
    {
        var culture = new PlantCulture { Id = 1 };
        var sort = new PlantSort { Id = 1, Producer = new Producer { Id = 1 } };
        var plant = new Plant { Id = 0, PlantCulture = culture, PlantSort = sort };
        var addedPlant = new Plant { Id = 1, PlantCulture = culture, PlantSort = sort };
        _plants.AddAsync(plant).Returns(addedPlant);

        var seedsInfo = new SeedsInfo { Id = 1 };
        var returnedSeed = new Seed { Id = 1, Plant = addedPlant, SeedsInfo = seedsInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _plants.Received(1).AddAsync(plant);
    }

    [Fact]
    public async Task MakeASeed_ExistingPlant_SkipsPlantAdd()
    {
        var plant = new Plant
        {
            Id = 7,
            PlantCulture = new PlantCulture { Id = 1 },
            PlantSort = new PlantSort { Id = 1, Producer = new Producer { Id = 1 } }
        };
        var seedsInfo = new SeedsInfo { Id = 1 };
        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = seedsInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _plants.DidNotReceive().AddAsync(Arg.Any<Plant>());
    }

    [Fact]
    public async Task MakeASeed_NewSeedsInfo_AddsSeedsInfo()
    {
        var plant = new Plant
        {
            Id = 1,
            PlantCulture = new PlantCulture { Id = 1 },
            PlantSort = new PlantSort { Id = 1, Producer = new Producer { Id = 1 } }
        };
        var seedsInfo = new SeedsInfo { Id = 0 };
        var addedInfo = new SeedsInfo { Id = 1 };
        _info.AddAsync(seedsInfo).Returns(addedInfo);

        var returnedSeed = new Seed { Id = 1, Plant = plant, SeedsInfo = addedInfo };
        _seeds.AddAsync(Arg.Any<Seed>()).Returns(returnedSeed);

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        await _info.Received(1).AddAsync(seedsInfo);
    }

    [Fact]
    public async Task MakeASeed_SetsBidirectionalRelationship()
    {
        var plant = new Plant
        {
            Id = 1,
            PlantCulture = new PlantCulture { Id = 1 },
            PlantSort = new PlantSort { Id = 1, Producer = new Producer { Id = 1 } }
        };
        var seedsInfo = new SeedsInfo { Id = 1 };

        Seed? capturedSeed = null;
        _seeds.AddAsync(Arg.Do<Seed>(s => capturedSeed = s)).Returns(c => c.Arg<Seed>());

        var service = CreateService();
        await service.MakeASeed(plant, seedsInfo);

        Assert.NotNull(capturedSeed);
        Assert.Same(capturedSeed, capturedSeed!.SeedsInfo.Seed);
    }

    // ── UpdateSeed ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSeed_CallsRepositoryUpdateAsync()
    {
        var seed = new Seed { Id = 1 };
        var service = CreateService();

        await service.UpdateSeed(seed);

        await _seeds.Received(1).UpdateAsync(seed);
    }

    [Fact]
    public async Task UpdateSeed_ReturnsSameSeed()
    {
        var seed = new Seed { Id = 5 };
        var service = CreateService();

        var result = await service.UpdateSeed(seed);

        Assert.Same(seed, result);
    }

    // ── DeleteSeed ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSeed_CallsRepositoryRemoveAsync()
    {
        var seed = new Seed { Id = 3 };
        var service = CreateService();

        await service.DeleteSeed(seed);

        await _seeds.Received(1).RemoveAsync(3);
    }

    // ── UpdateSort / UpdateCulture / UpdateProducer ───────────────────────────

    [Fact]
    public async Task UpdateSort_CallsRepositoryUpdateAsync()
    {
        var plantSort = new PlantSort { Id = 1 };
        var service = CreateService();

        await service.UpdateSort(plantSort);

        await _sort.Received(1).UpdateAsync(plantSort);
    }

    [Fact]
    public async Task UpdateCulture_CallsRepositoryUpdateAsync()
    {
        var plantCulture = new PlantCulture { Id = 1 };
        var service = CreateService();

        await service.UpdateCulture(plantCulture);

        await _culture.Received(1).UpdateAsync(plantCulture);
    }

    [Fact]
    public async Task UpdateProducer_CallsRepositoryUpdateAsync()
    {
        var prod = new Producer { Id = 1 };
        var service = CreateService();

        await service.UpdateProducer(prod);

        await _producer.Received(1).UpdateAsync(prod);
    }
}
