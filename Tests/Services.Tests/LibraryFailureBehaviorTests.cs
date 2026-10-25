using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Libraries;
using BonfireDB.Libraries;

namespace Services.Tests;

public class LibraryFailureBehaviorTests
{
    [Theory]
    [InlineData("Sort", true)]
    [InlineData("Sort", false)]
    [InlineData("Culture", true)]
    [InlineData("Culture", false)]
    [InlineData("Producer", true)]
    [InlineData("Producer", false)]
    public async Task SaveFailure_ReturnsExpectedErrorInsteadOfThrowing(string kind, bool concurrency)
    {
        var uow = Substitute.For<IUnitOfWork>();
        var sorts = Substitute.For<IRepository<PlantSort>>(); var cultures = Substitute.For<IRepository<PlantCulture>>(); var producers = Substitute.For<IRepository<Producer>>();
        uow.Repository<PlantSort>().Returns(sorts); uow.Repository<PlantCulture>().Returns(cultures); uow.Repository<Producer>().Returns(producers);
        sorts.GetAsync(1).Returns(new PlantSort { Id = 1, Name = "Сорт", Producer = new Producer() });
        cultures.GetAsync(1).Returns(new PlantCulture { Id = 1, Name = "Культура" });
        producers.GetAsync(1).Returns(new Producer { Id = 1, Name = "Производитель" });
        Exception failure = concurrency ? new DbUpdateConcurrencyException() : new DbUpdateException("DB", new Exception("Причина"));
        uow.SaveChangesAsync().Returns<int>(_ => throw failure);
        var factory = uow.ToFactory();
        var service = new LibraryService(new DbLibraryReader(factory), new UpdateCulture(new DbCultureWriter(factory)), new UpdateProducer(new DbProducerWriter(factory)), new UpdateSort(new DbSortWriter(factory)));
        var error = kind switch
        {
            "Sort" => (await service.UpdateSortAsync(new SortEditModel { Id = 1, Name = "Новый" })).Error,
            "Culture" => (await service.UpdateCultureAsync(new CultureEditModel { Id = 1, Name = "Новая", Class = "Овощи" })).Error,
            _ => (await service.UpdateProducerAsync(1, "Новый")).Error
        };
        Assert.Contains(concurrency ? "другим процессом" : "Причина", error);
    }
}
