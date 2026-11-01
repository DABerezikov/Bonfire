using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record GardenPlanResultFromViewModel(int Id, string Name, int Year, Option<string> Description);

public interface IGardenPlanCreationWriter
{
    Task<Result<GardenPlanResultFromViewModel>> CreateAsync(GardenPlanDefinition definition,
        CancellationToken cancellationToken);
}

public interface ICreateGardenPlan
{
    Task<Result<GardenPlanResultFromViewModel>> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class CreateGardenPlan(IGardenPlanCreationWriter writer, TimeProvider clock) : ICreateGardenPlan
{
    public Task<Result<GardenPlanResultFromViewModel>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return writer.CreateAsync(new GardenPlanDefinition(clock.GetLocalNow().Year), cancellationToken);
    }
}