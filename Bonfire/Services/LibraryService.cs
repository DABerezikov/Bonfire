using System;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.Services;

internal class LibraryService(IUnitOfWorkFactory uowFactory) : ILibraryService
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

        LibraryMapper.ApplyTo(model, entity);
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
        await using var uow = uowFactory.Create();
        var entity = await uow.Repository<PlantCulture>().GetAsync(model.Id);
        if (entity is null)
            return Result.Failure<PlantCulture>("Культура не найдена (удалёна?)");

        LibraryMapper.ApplyTo(model, entity);
        try
        {
            await uow.Repository<PlantCulture>().UpdateAsync(entity);
            await uow.SaveChangesAsync();
            return Result.Success(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PlantCulture>("Запись изменена другим процессом");
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure<PlantCulture>($"Ошибка БД: {ex.InnerException?.Message ?? ex.Message}");
        }
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
