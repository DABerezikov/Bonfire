using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenCreation
{
    public GardenPlanId PlanId { get; }
    public string Name { get; }
    public PlotDimensions Dimensions { get; }
    private GardenCreation(GardenPlanId planId, string name, PlotDimensions dimensions)
        => (PlanId, Name, Dimensions) = (planId, name, dimensions);

    public static Result<GardenCreation> Create(int planId, string name, double widthMeters, double heightMeters, double pixelsPerMeter)
        => GardenPlanId.Create(planId).Bind(id => Result.From(name, name is null ? "Название участка отсутствует" : null)
            .Bind(text => PlotDimensions.Create(widthMeters, heightMeters, pixelsPerMeter)
                .Bind(dimensions => Result.Success(new GardenCreation(id, text, dimensions)))));
}
