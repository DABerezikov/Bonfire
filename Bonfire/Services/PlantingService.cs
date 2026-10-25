using System;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Seeds;
using Bonfire.Models;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class PlantingService(IPlantInSpot planting) : IPlantingService
{
    public Task<Result<PlantingResultFromViewModel>> PlantAsync(PlantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var source = request.Kind switch
        {
            PlantSourceKind.Seedling => SeedlingId.Create(request.EntityId).Bind(id => Result.Success<PlantingSource>(new PlantingSource.Seedlings(id))),
            PlantSourceKind.Seed => SeedId.Create(request.EntityId).Bind(id => Result.Success<PlantingSource>(new PlantingSource.Seeds(id,
                request.IsWeightBased ? SeedUnit.Grams : SeedUnit.Pieces))),
            _ => Result.Failure<PlantingSource>("Неизвестный источник посадки")
        };
        return source.Match(value => planting.ExecuteAsync(new(request.SpotId, value, request.Amount, request.PlantedDate,
            request.PlantName, request.PlantPlace.ToOption()), cancellationToken),
            error => Task.FromResult(Result.Failure<PlantingResultFromViewModel>(error)));
    }
}
