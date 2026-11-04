using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.SeedEditing;

namespace BonfireDB.Libraries;

public static class LibraryReadDbMapper
{
    public static CultureDetailsFromViewModel Read(PlantCulture culture) =>
        new(culture.Id, culture.Name, culture.Class);

    public static ProducerDetailsFromViewModel Read(Producer producer) => new(producer.Id, producer.Name);

    public static Result<SortReadFromViewModel> Read(PlantSort sort)
        => sort.Producer is not { } producer
            ? Result.Failure<SortReadFromViewModel>($"Сорт {sort.Id}: производитель отсутствует")
            : Result.Success(new SortReadFromViewModel(sort.Id, sort.Name, Read(producer),
                (sort.Plants.FirstOrDefault()?.PlantCulture.Id).ToOption(), SortCharacteristicsDbMapper.Read(sort)));
}