using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class GardenService(
    IResizeGrid resizeGrid,
    IChangeGardenState changeGardenState,
    ISaveGardenGeometry saveGardenGeometry,
    IEditElementProperties editElementProperties,
    IEditGreenhouseProperties editGreenhouseProperties,
    IEditGardenProperties editGardenProperties,
    ISetGardenLock setGardenLock,
    IDeleteGardenElement deleteGardenElement,
    IDeleteGreenhouse deleteGreenhouse,
    IDeleteGarden deleteGarden,
    IDeleteGardenPlan deleteGardenPlan,
    ICreateGardenPlan createGardenPlan,
    ICreateGarden createGarden,
    ICreateGardenObjects createGardenObjects,
    IGardenReader gardenReader) : IGardenService
{
    public Task<Result<GeometryResultFromViewModel>> SaveGeometryAsync(SaveGardenGeometryFromViewModel request)
        => saveGardenGeometry.ExecuteAsync(request);

    public Task<Result<ElementPropertiesResultFromViewModel>> EditElementPropertiesAsync(
        EditElementPropertiesFromViewModel request)
        => editElementProperties.ExecuteAsync(request);

    public Task<Result<GreenhousePropertiesResultFromViewModel>> EditGreenhousePropertiesAsync(
        EditGreenhousePropertiesFromViewModel request)
        => editGreenhouseProperties.ExecuteAsync(request);

    public Task<Result<GardenPropertiesResultFromViewModel>> EditGardenPropertiesAsync(
        EditGardenPropertiesFromViewModel request)
        => editGardenProperties.ExecuteAsync(request);

    public Task<Result<GardenLockResultFromViewModel>> SetLockAsync(SetGardenLockFromViewModel request)
        => setGardenLock.ExecuteAsync(request);

    public Task<Result<ElementDeletionResultFromViewModel>> DeleteGardenElementAsync(
        DeleteGardenElementFromViewModel request)
        => deleteGardenElement.ExecuteAsync(request);

    public Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteGreenhouseByIdAsync(
        DeleteGreenhouseFromViewModel request)
        => deleteGreenhouse.ExecuteAsync(request);

    public Task<Result<GardenDeletionResultFromViewModel>> DeleteGardenByIdAsync(DeleteGardenFromViewModel request)
        => deleteGarden.ExecuteAsync(request);

    public Task<Result<GardenPlanDeletionResultFromViewModel>> DeletePlanByIdAsync(
        DeleteGardenPlanFromViewModel request)
        => deleteGardenPlan.ExecuteAsync(request);

    public Task<Result<GardenPlanResultFromViewModel>> CreateCurrentPlanAsync() => createGardenPlan.ExecuteAsync();

    public Task<Result<GardenPropertiesResultFromViewModel>>
        CreateGardenFromRequestAsync(CreateGardenFromViewModel request) => createGarden.ExecuteAsync(request);

    public Task<Result<ElementCreationResultFromViewModel>>
        CreateElementFromRequestAsync(CreateGardenElementFromViewModel request) =>
        createGardenObjects.CreateElementAsync(request);

    public Task<Result<GreenhouseCreationResultFromViewModel>>
        CreateGreenhouseFromRequestAsync(CreateGreenhouseFromViewModel request) =>
        createGardenObjects.CreateGreenhouseAsync(request);

    public Task<Result<ImmutableArray<GardenPlanResultFromViewModel>>> ReadPlansAsync() =>
        gardenReader.ReadPlansAsync(default);

    public Task<Result<ImmutableArray<GardenReadFromViewModel>>> ReadGardensAsync(int planId)
        => GardenPlanId.Create(planId).Match(id => gardenReader.ReadGardensAsync(id, default),
            error => Task.FromResult(Result.Failure<ImmutableArray<GardenReadFromViewModel>>(error)));

    public Task<Result<GardenStateResultFromViewModel>> ChangeStateAsync(ChangeGardenStateFromViewModel request,
        CancellationToken cancellationToken = default)
        => changeGardenState.ExecuteAsync(request, cancellationToken);

    public Task<Result<GridResultFromViewModel>> ResizeGridAsync(ResizeGridFromViewModel request,
        CancellationToken cancellationToken = default)
        => resizeGrid.ExecuteAsync(request, cancellationToken);
}