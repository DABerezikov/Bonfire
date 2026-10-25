using System;
using Bonfire.Domain.Functional;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Libraries;

namespace Bonfire.Services;

internal class LibraryService(IUnitOfWorkFactory uowFactory, IUpdateCulture updateCulture) : ILibraryService
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

    public async Task<Result<PlantSort>> UpdateSortAsync(SortEditModel model)
    {
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<PlantSort>().GetAsync(model.Id);
        if (entity is null)
            return Result.Failure<PlantSort>("Сорт не найден (удалён?)");

        if (string.IsNullOrWhiteSpace(model.Name))
            return Result.Failure<PlantSort>("Название не может быть пустым");
        PlantCulture? culture = null;
        if (model.CultureId.HasValue)
        {
            culture = await uow.Repository<PlantCulture>().GetAsync(model.CultureId.Value);
            if (culture is null) return Result.Failure<PlantSort>("Культура не найдена");
        }
        entity.Name = model.Name.Trim();
        if (culture is not null)
        {
            var plants = await uow.Repository<Plant>().Items
                .Where(p => p.PlantSort.Id == entity.Id).ToListAsync();
            foreach (var plant in plants)
                plant.PlantCulture = culture;
        }
        try
        {
            await uow.Repository<PlantSort>().UpdateAsync(entity);
            await uow.SaveChangesAsync();
            return Result.Success(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PlantSort>("Запись изменена другим процессом");
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure<PlantSort>($"Ошибка БД: {ex.InnerException?.Message ?? ex.Message}");
        }
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
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<Producer>().GetAsync(id);
        if (entity is null)
            return Result.Failure<Producer>("Производитель не найден (удалён?)");

        entity.Name = name;
        try
        {
            await uow.Repository<Producer>().UpdateAsync(entity);
            await uow.SaveChangesAsync();
            return Result.Success(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<Producer>("Запись изменена другим процессом");
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure<Producer>($"Ошибка БД: {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}
