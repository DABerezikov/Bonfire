using System;
using Bonfire.Domain.Functional;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Bonfire.Application.Libraries;

namespace Bonfire.Services;

internal class LibraryService(IUnitOfWorkFactory uowFactory, IUpdateCulture updateCulture, IUpdateProducer updateProducer, IUpdateSort updateSort) : ILibraryService
{
    public async Task<Result<PlantSort>> GetSortAsync(int id)
    {
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<PlantSort>().GetAsync(id);
        return entity is null
            ? Result.Failure<PlantSort>("Сорт не найден")
            : Result.Success(entity);
    }

    public async Task<Result<PlantCulture>> GetCultureAsync(int id)
    {
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<PlantCulture>().GetAsync(id);
        return entity is null
            ? Result.Failure<PlantCulture>("Культура не найдена")
            : Result.Success(entity);
    }

    public async Task<Result<Producer>> GetProducerAsync(int id)
    {
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<Producer>().GetAsync(id);
        return entity is null
            ? Result.Failure<Producer>("Производитель не найден")
            : Result.Success(entity);
    }

    public Task<Result<SortDetailsFromViewModel>> UpdateSortAsync(SortEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Name is not { } name)
            return Task.FromResult(Result.Failure<SortDetailsFromViewModel>("Название не может быть пустым"));
        var culture = model.CultureId.ToOption();
        return updateSort.ExecuteAsync(new UpdateSortFromViewModel(model.Id, name, culture));
    }

    public async Task<Result<PlantCulture>> UpdateCultureAsync(CultureEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Name is not { } name)
            return Result.Failure<PlantCulture>("Название не может быть пустым");
        var result = await updateCulture.ExecuteAsync(new UpdateCultureFromViewModel(model.Id, name, model.Class));
        // Совместимость прежнего UI: EF-тип появляется только на внешней границе, не в Application.
        return result.Bind(saved => Result.Success(new PlantCulture
        {
            Id = saved.Id, Name = saved.Name, Class = saved.Class
        }));
    }

    public async Task<Result<Producer>> UpdateProducerAsync(int id, string name)
    {
        var result = await updateProducer.ExecuteAsync(new UpdateProducerFromViewModel(id, name));
        return result.Bind(saved => Result.Success(new Producer { Id = saved.Id, Name = saved.Name }));
    }
}
