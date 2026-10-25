using Bonfire.Models;
using Bonfire.Services.Interfaces;
using Bonfire.Application.Libraries;
using BonfireDB.Libraries;

namespace Services.Tests;

public class LibraryServiceTests
{
    private readonly IRepository<PlantSort> _sorts = Substitute.For<IRepository<PlantSort>>();
    private readonly IRepository<PlantCulture> _cultures = Substitute.For<IRepository<PlantCulture>>();
    private readonly IRepository<Producer> _producers = Substitute.For<IRepository<Producer>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public LibraryServiceTests()
    {
        _uow.Repository<PlantSort>().Returns(_sorts);
        _uow.Repository<PlantCulture>().Returns(_cultures);
        _uow.Repository<Producer>().Returns(_producers);
    }

    private LibraryService CreateService()
    {
        var factory = _uow.ToFactory();
        return new(factory, new UpdateCulture(new DbCultureWriter(factory)), new UpdateProducer(new DbProducerWriter(factory)));
    }

    // ── GetSortAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSortAsync_ExistingId_ReturnsSuccess()
    {
        var sort = new PlantSort { Id = 1, Name = "Черри", Producer = new Producer() };
        _sorts.GetAsync(1).Returns(sort);

        var result = await CreateService().GetSortAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Черри", result.Value!.Name);
    }

    [Fact]
    public async Task GetSortAsync_MissingId_ReturnsFailure()
    {
        _sorts.GetAsync(99).Returns((PlantSort?)null);

        var result = await CreateService().GetSortAsync(99);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
    }

    // ── UpdateSortAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSortAsync_LibraryEdit_ChangesNameAndPreservesCharacteristics()
    {
        var entity = new PlantSort { Id = 1, Name = "Старый", Description = "Сохранённое описание", PlantHeight = 80, Producer = new Producer() };
        _sorts.GetAsync(1).Returns(entity);

        var model = new SortEditModel
        {
            Id = 1,
            Name = "Новый",
            Description = "Описание",
            MinGerminationTime = 10,
            MaxGerminationTime = 20,
            AgeOfSeedlings = 45,
            GrowingSeason = 90,
            LandingPattern = 30,
            PlantHeight = 150,
            PlantColor = "#FF0000"
        };

        var result = await CreateService().UpdateSortAsync(model);

        Assert.True(result.IsSuccess);
        Assert.Equal("Новый", entity.Name);
        Assert.Equal("Сохранённое описание", entity.Description);
        Assert.Equal(80, entity.PlantHeight);
    }

    [Fact]
    public async Task UpdateSortAsync_MissingSort_ReturnsFailure()
    {
        _sorts.GetAsync(99).Returns((PlantSort?)null);
        var model = new SortEditModel { Id = 99, Name = "X" };

        var result = await CreateService().UpdateSortAsync(model);

        Assert.True(result.IsFailure);
    }

    // ── GetCultureAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetCultureAsync_ExistingId_ReturnsSuccess()
    {
        var culture = new PlantCulture { Id = 2, Name = "Томат", Class = "Овощи" };
        _cultures.GetAsync(2).Returns(culture);

        var result = await CreateService().GetCultureAsync(2);

        Assert.True(result.IsSuccess);
        Assert.Equal("Томат", result.Value!.Name);
    }

    [Fact]
    public async Task GetCultureAsync_MissingId_ReturnsFailure()
    {
        _cultures.GetAsync(99).Returns((PlantCulture?)null);

        var result = await CreateService().GetCultureAsync(99);

        Assert.True(result.IsFailure);
    }

    // ── UpdateCultureAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCultureAsync_ExistingCulture_AppliesFields()
    {
        var entity = new PlantCulture { Id = 2, Name = "Старый", Class = "Зелень" };
        _cultures.GetAsync(2).Returns(entity);

        var model = new CultureEditModel { Id = 2, Name = "Новый", Class = "Овощи" };

        var result = await CreateService().UpdateCultureAsync(model);

        Assert.True(result.IsSuccess);
        Assert.Equal("Новый", entity.Name);
        Assert.Equal("Овощи", entity.Class);
    }

    [Fact]
    public async Task UpdateCultureAsync_MissingCulture_ReturnsFailure()
    {
        _cultures.GetAsync(99).Returns((PlantCulture?)null);
        var model = new CultureEditModel { Id = 99, Name = "X", Class = "Овощи" };

        var result = await CreateService().UpdateCultureAsync(model);

        Assert.True(result.IsFailure);
    }

    // ── GetProducerAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducerAsync_ExistingId_ReturnsSuccess()
    {
        var producer = new Producer { Id = 3, Name = "Гавриш" };
        _producers.GetAsync(3).Returns(producer);

        var result = await CreateService().GetProducerAsync(3);

        Assert.True(result.IsSuccess);
        Assert.Equal("Гавриш", result.Value!.Name);
    }

    [Fact]
    public async Task GetProducerAsync_MissingId_ReturnsFailure()
    {
        _producers.GetAsync(99).Returns((Producer?)null);

        var result = await CreateService().GetProducerAsync(99);

        Assert.True(result.IsFailure);
    }

    // ── UpdateProducerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProducerAsync_ExistingProducer_UpdatesName()
    {
        var entity = new Producer { Id = 3, Name = "Старый" };
        _producers.GetAsync(3).Returns(entity);

        var result = await CreateService().UpdateProducerAsync(3, "Новый");

        Assert.True(result.IsSuccess);
        Assert.Equal("Новый", entity.Name);
    }

    [Fact]
    public async Task UpdateProducerAsync_MissingProducer_ReturnsFailure()
    {
        _producers.GetAsync(99).Returns((Producer?)null);

        var result = await CreateService().UpdateProducerAsync(99, "X");

        Assert.True(result.IsFailure);
    }
}
