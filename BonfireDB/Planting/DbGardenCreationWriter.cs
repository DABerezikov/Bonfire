using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenCreationWriter(IUnitOfWorkFactory units) : IGardenCreationWriter
{
    public async Task<Result<GardenPropertiesResultFromViewModel>> CreateAsync(GardenCreation creation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(creation);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        if (!await unit.Repository<GardenPlan>().Items.AnyAsync(p => p.Id == creation.PlanId.Value, cancellationToken)
                .ConfigureAwait(false))
            return Result.Failure<GardenPropertiesResultFromViewModel>("Исходный план уже удалён");
        var row = new Garden
        {
            GardenPlanId = creation.PlanId.Value, Name = creation.Name,
            WidthMeters = creation.Dimensions.WidthMeters, HeightMeters = creation.Dimensions.HeightMeters,
            CanvasWidth = creation.Dimensions.Canvas.Width, CanvasHeight = creation.Dimensions.Canvas.Height
        };
        try
        {
            await unit.Repository<Garden>().AddAsync(row, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GardenPropertiesResultFromViewModel(row.Id,
                new GardenEditorValues(row.Name, row.Note.ToOption(), row.Address.ToOption(), row.WidthMeters,
                    row.HeightMeters), creation.Dimensions.Canvas));
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenPropertiesResultFromViewModel>(
                $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}