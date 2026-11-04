using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbLibraryReader(IUnitOfWorkFactory units) : ILibraryReader
{
    public Task<Result<SortReadFromViewModel>> ReadSortAsync(SortId id, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var row = await unit.Repository<PlantSort>().Rows.Include(sort => sort.Producer)
                .Include(sort => sort.Plants).ThenInclude(plant => plant.PlantCulture).AsNoTracking().AsSplitQuery()
                .SingleOrDefaultAsync(sort => sort.Id == id.Value, cancellationToken).ConfigureAwait(false);
            return row is null
                ? Result.Failure<SortReadFromViewModel>("Сорт не найден")
                : LibraryReadDbMapper.Read(row);
        }, "библиотеки");

    public Task<Result<CultureDetailsFromViewModel>> ReadCultureAsync(CultureId id, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var row = await unit.Repository<PlantCulture>().Rows.AsNoTracking()
                .SingleOrDefaultAsync(culture => culture.Id == id.Value, cancellationToken).ConfigureAwait(false);
            return row is null
                ? Result.Failure<CultureDetailsFromViewModel>("Культура не найдена")
                : Result.Success(LibraryReadDbMapper.Read(row));
        }, "библиотеки");

    public Task<Result<ProducerDetailsFromViewModel>> ReadProducerAsync(ProducerId id,
        CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var row = await unit.Repository<Producer>().Rows.AsNoTracking()
                .SingleOrDefaultAsync(producer => producer.Id == id.Value, cancellationToken).ConfigureAwait(false);
            return row is null
                ? Result.Failure<ProducerDetailsFromViewModel>("Производитель не найден")
                : Result.Success(LibraryReadDbMapper.Read(row));
        }, "библиотеки");
}