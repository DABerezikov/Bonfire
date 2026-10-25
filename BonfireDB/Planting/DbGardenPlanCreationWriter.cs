using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenPlanCreationWriter(IUnitOfWorkFactory units) : IGardenPlanCreationWriter
{
    public async Task<Result<GardenPlanResultFromViewModel>> CreateAsync(GardenPlanDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var row = new GardenPlan { Name = definition.Name, Year = definition.Year };
        try
        {
            await unit.Repository<GardenPlan>().AddAsync(row, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GardenPlanResultFromViewModel(row.Id, row.Name, row.Year, row.Description.ToOption()));
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenPlanResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
