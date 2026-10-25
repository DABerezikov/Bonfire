using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.SpotStates;
using BonfireDB.SeedEditing;
using BonfireDB.Sowing;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbPlantingWriter(IUnitOfWorkFactory units) : IPlantingWriter
{
    public async Task<Result<PlantingResultFromViewModel>> ApplyAsync(SpotPlanting planting, string lunarPhase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(planting);
        ArgumentNullException.ThrowIfNull(lunarPhase);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var spot = await unit.Repository<PlantingSpot>().GetAsync(planting.SpotId, cancellationToken).ConfigureAwait(false);
        if (spot is null) return Result.Failure<PlantingResultFromViewModel>("Ячейка уже удалена");
        var state = new PlantedSpotState();
        if (!spot.State.CanTransitionTo(state))
            return Result.Failure<PlantingResultFromViewModel>("Текущее состояние ячейки запрещает посадку");

        var prepared = await (planting.Source switch
        {
            PlantingSource.Seedlings source => FromSeedlingsAsync(unit, planting, source, lunarPhase, cancellationToken),
            PlantingSource.Seeds source => FromSeedsAsync(unit, planting, source, lunarPhase, cancellationToken),
            _ => throw new System.Diagnostics.UnreachableException()
        }).ConfigureAwait(false);

        return await prepared.Match(async change =>
        {
            spot.TransitionTo(state);
            spot.Note = planting.Label;
            spot.PlantedDate = planting.Date;
            spot.SeedlingInfo = change.Info;
            try
            {
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(new PlantingResultFromViewModel(spot.Id, change.Info.Id, change.Consumed, change.Remaining));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<PlantingResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<PlantingResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<PlantingResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<PreparedPlanting>> FromSeedlingsAsync(IUnitOfWork unit, SpotPlanting planting,
        PlantingSource.Seedlings source, string phase, CancellationToken cancellationToken)
    {
        var batch = await unit.Repository<Seedling>().GetAsync(source.Id.Value, cancellationToken).ConfigureAwait(false);
        if (batch is null) return Result.Failure<PreparedPlanting>("Партия рассады уже удалена");
        if (batch.IsPlantedInBed) return Result.Failure<PreparedPlanting>("Эта партия уже высажена в грядку");
        return SeedlingBatchDbMapper.Read(batch).Bind(model => model.PlantOut(planting.Amount.Value)).Bind(change =>
        {
            batch.PlantedOut = change.Remaining.PlantedOut;
            var consumed = SeedAmount.Create(change.Quantity, SeedUnit.Pieces).ValueOrThrow();
            var remaining = SeedAmount.Create(change.Remaining.AvailableCount, SeedUnit.Pieces).ValueOrThrow();
            return Result.Success(new PreparedPlanting(CreateBatch(unit, batch.Plant, batch.SeedId, consumed, planting, phase), consumed, remaining));
        });
    }

    private static async Task<Result<PreparedPlanting>> FromSeedsAsync(IUnitOfWork unit, SpotPlanting planting,
        PlantingSource.Seeds source, string phase, CancellationToken cancellationToken)
    {
        var seed = await unit.Repository<Seed>().GetAsync(source.Id.Value, cancellationToken).ConfigureAwait(false);
        if (seed is null) return Result.Failure<PreparedPlanting>("Пакет семян уже удалён");
        return SeedPackageDbMapper.ReadStock(seed.SeedsInfo, source.Unit).Bind(stock => stock.SowUpTo(planting.Amount)).Bind(change =>
        {
            if (source.Unit == SeedUnit.Pieces)
                seed.SeedsInfo.AmountSeeds = change.Remaining.Available.Value;
            else
                seed.SeedsInfo.AmountSeedsWeight = change.Remaining.Available.Value;
            return Result.Success(new PreparedPlanting(CreateBatch(unit, seed.Plant, seed.Id, change.Sown, planting, phase), change.Sown, change.Remaining.Available));
        });
    }

    private static SeedlingInfo CreateBatch(IUnitOfWork unit, Plant plant, int seedId, SeedAmount amount, SpotPlanting planting, string phase)
    {
        var place = planting.Place.ToNullable();
        var info = new SeedlingInfo
        {
            LandingDate = planting.Date, LunarPhase = phase, SeedlingNumber = 0,
            SeedlingSource = PlantSources.FromSeeds, PlantPlace = place
        };
        unit.Repository<Seedling>().Add(new Seedling
        {
            Plant = plant, SeedId = seedId, LandingDate = planting.Date, LunarPhase = phase,
            SeedlingSource = PlantSources.FromSeeds, PlantPlace = place, IsPlantedInBed = true,
            Quantity = amount.Unit == SeedUnit.Pieces ? amount.Value : 0,
            Weight = amount.Unit == SeedUnit.Grams ? amount.Value : 0, SeedlingInfos = [info]
        });
        return info;
    }

    private sealed record PreparedPlanting(SeedlingInfo Info, SeedAmount Consumed, SeedAmount Remaining);
}
