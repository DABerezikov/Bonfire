using System.Collections.Immutable;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

public static class GardenReadDbMapper
{
    public static GardenPlanResultFromViewModel ReadPlan(GardenPlan plan)
        => new(plan.Id, plan.Name, plan.Year, plan.Description.ToOption());

    public static Result<GardenReadFromViewModel> ReadGarden(Garden garden)
        => CanvasRectangle.Create(0, 0, garden.CanvasWidth, garden.CanvasHeight).Bind(canvas =>
            PlotDimensions.Create(garden.WidthMeters, garden.HeightMeters, 1).Bind(_ =>
                garden.Elements.Traverse(ReadElement).Bind(elements =>
                    garden.Greenhouses.Traverse(ReadGreenhouse).Bind(houses =>
                        Result.Success(new GardenReadFromViewModel(new(garden.Id,
                            new(garden.Name, garden.Note.ToOption(), garden.Address.ToOption(), garden.WidthMeters, garden.HeightMeters), canvas), elements, houses))))))
            .Match(Result.Success, error => Result.Failure<GardenReadFromViewModel>($"Участок {garden.Id}: {error}"));

    public static Result<ElementReadFromViewModel> ReadElement(GardenElement element)
        => ElementPropertiesDbMapper.Receipt(element).Bind(properties =>
            !double.IsFinite(element.Rotation) ? Result.Failure<ElementReadFromViewModel>("Поворот должен быть конечным")
                : GridSize.Create(element.GridRows, element.GridColumns).Bind(_ =>
                    Result.Success(new ElementReadFromViewModel(properties, element.GetType().Name, element.Rotation,
                        element.StateTypeName, element.IsLocked, element.GridRows, element.GridColumns,
                        element is FlowerBed flower ? flower.Shape.ToOption() : Option.None<string>(),
                        element.PlantingSpots.OrderBy(s => s.Row).ThenBy(s => s.Column).Select(DbGridMapper.Receipt).ToImmutableArray()))))
            .Match(Result.Success, error => Result.Failure<ElementReadFromViewModel>($"Элемент {element.Id}: {error}"));

    public static Result<GreenhouseReadFromViewModel> ReadGreenhouse(Greenhouse house)
        => GreenhousePropertiesDbMapper.Read(house).Bind(properties =>
            PlotDimensions.Create(house.WidthMeters, house.HeightMeters, 1).Bind(_ =>
                CanvasRectangle.Create(house.X, house.Y, house.DisplayWidth, house.DisplayHeight).Bind(rectangle =>
                    !double.IsFinite(house.Rotation) ? Result.Failure<GreenhouseReadFromViewModel>("Поворот должен быть конечным")
                        : house.Elements.Traverse(ReadElement).Bind(elements => Result.Success(new GreenhouseReadFromViewModel(
                            GreenhousePropertiesDbMapper.Receipt(properties), rectangle, house.Rotation, house.StateTypeName, house.IsLocked, elements))))))
            .Match(Result.Success, error => Result.Failure<GreenhouseReadFromViewModel>($"Теплица {house.Id}: {error}"));
}
