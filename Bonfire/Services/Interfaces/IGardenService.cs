using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Services.Interfaces;

public interface IGardenService
{
    Task<Result<GeometryResultFromViewModel>> SaveGeometryAsync(SaveGardenGeometryFromViewModel request);
    Task<Result<ElementPropertiesResultFromViewModel>> EditElementPropertiesAsync(EditElementPropertiesFromViewModel request);
    Task<Result<GreenhousePropertiesResultFromViewModel>> EditGreenhousePropertiesAsync(EditGreenhousePropertiesFromViewModel request);
    Task<Result<GardenPropertiesResultFromViewModel>> EditGardenPropertiesAsync(EditGardenPropertiesFromViewModel request);
    Task<Result<GardenLockResultFromViewModel>> SetLockAsync(SetGardenLockFromViewModel request);
    Task<Result<ElementDeletionResultFromViewModel>> DeleteGardenElementAsync(DeleteGardenElementFromViewModel request);
    Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteGreenhouseByIdAsync(DeleteGreenhouseFromViewModel request);
    Task<Result<GardenDeletionResultFromViewModel>> DeleteGardenByIdAsync(DeleteGardenFromViewModel request);
    Task<Result<GardenPlanDeletionResultFromViewModel>> DeletePlanByIdAsync(DeleteGardenPlanFromViewModel request);
    Task<Result<GardenPlanResultFromViewModel>> CreateCurrentPlanAsync();
    Task<Result<GardenPropertiesResultFromViewModel>> CreateGardenFromRequestAsync(CreateGardenFromViewModel request);
    Task<Result<ElementCreationResultFromViewModel>> CreateElementFromRequestAsync(CreateGardenElementFromViewModel request);
    Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseFromRequestAsync(CreateGreenhouseFromViewModel request);
    Task<Result<ImmutableArray<GardenPlanResultFromViewModel>>> ReadPlansAsync();
    Task<Result<ImmutableArray<GardenReadFromViewModel>>> ReadGardensAsync(int planId);
    Task<Result<GardenStateResultFromViewModel>> ChangeStateAsync(ChangeGardenStateFromViewModel request, CancellationToken cancellationToken = default);
    Task<Result<GridResultFromViewModel>> ResizeGridAsync(ResizeGridFromViewModel request, CancellationToken cancellationToken = default);
}
