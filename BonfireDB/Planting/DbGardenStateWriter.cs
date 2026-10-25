using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.States;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenStateWriter(IUnitOfWorkFactory units) : IGardenStateWriter
{
    public async Task<Result<GardenStateResultFromViewModel>> ApplyAsync(GardenStateChange change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        return await (change.Target switch
        {
            StateChangeTarget.Element element => ChangeElementAsync(unit, change.EntityId, element.State, cancellationToken),
            StateChangeTarget.Greenhouse greenhouse => ChangeGreenhouseAsync(unit, change.EntityId, greenhouse.State, cancellationToken),
            StateChangeTarget.Spot spot => ChangeSpotAsync(unit, change.EntityId, spot.Change, cancellationToken),
            _ => throw new System.Diagnostics.UnreachableException()
        }).ConfigureAwait(false);
    }

    private static async Task<Result<GardenStateResultFromViewModel>> ChangeElementAsync(IUnitOfWork unit, int id, GardenElementState target, CancellationToken token)
    {
        var element = await unit.Repository<GardenElement>().GetAsync(id, token).ConfigureAwait(false);
        if (element is null) return Result.Failure<GardenStateResultFromViewModel>("Элемент уже удалён");
        return await element.State.TransitionTo(target).Match(state =>
        {
            element.StateTypeName = state.GetType().Name;
            return SaveAsync(unit, () => new GardenStateResultFromViewModel.Element(element.Id, state), token);
        }, error => Task.FromResult(Result.Failure<GardenStateResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<GardenStateResultFromViewModel>> ChangeGreenhouseAsync(IUnitOfWork unit, int id, GardenElementState target, CancellationToken token)
    {
        var greenhouse = await unit.Repository<Greenhouse>().GetAsync(id, token).ConfigureAwait(false);
        if (greenhouse is null) return Result.Failure<GardenStateResultFromViewModel>("Теплица уже удалена");
        return await greenhouse.State.TransitionTo(target).Match(state =>
        {
            greenhouse.StateTypeName = state.GetType().Name;
            return SaveAsync(unit, () => new GardenStateResultFromViewModel.Greenhouse(greenhouse.Id, state), token);
        }, error => Task.FromResult(Result.Failure<GardenStateResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<GardenStateResultFromViewModel>> ChangeSpotAsync(IUnitOfWork unit, int id, SpotChange change, CancellationToken token)
    {
        var spot = await unit.Repository<PlantingSpot>().GetAsync(id, token).ConfigureAwait(false);
        if (spot is null) return Result.Failure<GardenStateResultFromViewModel>("Ячейка уже удалена");
        return await PlantingCellDbMapper.Read(spot).Apply(change).Match(cell =>
        {
            PlantingCellDbMapper.Apply(cell, spot);
            return SaveAsync(unit, () => new GardenStateResultFromViewModel.Spot(DbGridMapper.Receipt(spot)), token);
        }, error => Task.FromResult(Result.Failure<GardenStateResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<GardenStateResultFromViewModel>> SaveAsync(IUnitOfWork unit, Func<GardenStateResultFromViewModel> receipt, CancellationToken token)
    {
        try
        {
            await unit.SaveChangesAsync(token).ConfigureAwait(false);
            return Result.Success(receipt());
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GardenStateResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenStateResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
