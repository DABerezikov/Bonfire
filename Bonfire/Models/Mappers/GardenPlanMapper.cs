using System.Collections.ObjectModel;
using System.Linq;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.States;

namespace Bonfire.Models.Mappers;

/// <summary>
/// Маппинг независимых снимков Application в модели UI.
/// Чистые функции без зависимостей от ViewModel — легко тестируются.
/// </summary>
internal static class GardenPlanMapper
{
    public static GardenElementFromViewModel MapElement(ElementCreationResultFromViewModel saved)
    {
        var state = new PlannedState();
        var element = new GardenElementFromViewModel
        {
            Id = saved.Properties.Id, ElementType = saved.ElementType,
            ContainerCanvasWidth = saved.Container.Width, ContainerCanvasHeight = saved.Container.Height,
            StateTypeName = nameof(PlannedState), StateDisplayName = state.DisplayName, StateColor = state.StatusColor,
            CanAddPlanting = state.CanAddPlanting, CanModifyGrid = state.CanModifyGrid
        };
        ElementEditorMapper.Apply(saved.Properties, element);
        return element;
    }

    public static GreenhouseFromViewModel MapGreenhouse(GreenhouseCreationResultFromViewModel saved)
    {
        var state = new PlannedState();
        var house = new GreenhouseFromViewModel
        {
            Id = saved.Properties.Id, ContainerCanvasWidth = saved.Container.Width, ContainerCanvasHeight = saved.Container.Height,
            StateTypeName = nameof(PlannedState), StateDisplayName = state.DisplayName, StateColor = state.StatusColor
        };
        GreenhouseEditorMapper.Apply(saved.Properties, house);
        CanvasGeometryMapper.Apply(saved.Rectangle, house);
        return house;
    }

    public static GardenPlanFromViewModel MapPlan(GardenPlanResultFromViewModel plan) => new()
    {
        Id = plan.Id, Name = plan.Name, Year = plan.Year, Description = plan.Description
    };

    public static GardenFromViewModel MapGarden(GardenPropertiesResultFromViewModel saved)
    {
        var garden = new GardenFromViewModel { Id = saved.Id };
        GardenEditorMapper.Apply(saved, garden);
        return garden;
    }

    public static PlantingSpotFromViewModel MapSpot(GridSpotFromViewModel spot) => new()
    {
        Id = spot.Id, Row = spot.Row, Column = spot.Column, StateTypeName = spot.StateTypeName,
        SeedlingInfoId = spot.SeedlingInfoId.ToNullable(), PlantedDate = spot.PlantedDate.ToNullable(),
        PlantLabel = spot.Note.ToNullable(), Note = spot.Note.ToNullable()
    };

    public static GardenFromViewModel MapGarden(GardenReadFromViewModel snapshot)
    {
        var garden = MapGarden(snapshot.Properties);
        garden.Elements = new(snapshot.Elements.Select(e => MapElement(e, garden.CanvasWidth, garden.CanvasHeight)));
        garden.Greenhouses = new(snapshot.Greenhouses.Select(h => MapGreenhouse(h, garden.CanvasWidth, garden.CanvasHeight)));
        foreach (var element in garden.Elements) { element.ContainerElements = garden.Elements; element.ContainerGreenhouses = garden.Greenhouses; }
        foreach (var house in garden.Greenhouses) { house.ContainerElements = garden.Elements; house.ContainerGreenhouses = garden.Greenhouses; }
        return garden;
    }

    public static GardenElementFromViewModel MapElement(ElementReadFromViewModel snapshot, double containerW = 0, double containerH = 0)
    {
        var state = GardenElementState.From(snapshot.StateTypeName);
        var element = new GardenElementFromViewModel
        {
            Id = snapshot.Properties.Id, ElementType = snapshot.ElementType, ContainerCanvasWidth = containerW, ContainerCanvasHeight = containerH,
            Rotation = snapshot.Rotation, IsLocked = snapshot.IsLocked, StateTypeName = snapshot.StateTypeName,
            StateDisplayName = state.DisplayName, StateColor = state.StatusColor, CanAddPlanting = state.CanAddPlanting, CanModifyGrid = state.CanModifyGrid,
            GridRows = snapshot.GridRows, GridColumns = snapshot.GridColumns, Shape = snapshot.Shape.ToNullable(),
            PlantingSpots = new(snapshot.Spots.Select(MapSpot))
        };
        ElementEditorMapper.Apply(snapshot.Properties, element);
        return element;
    }

    public static GreenhouseFromViewModel MapGreenhouse(GreenhouseReadFromViewModel snapshot, double containerW = 0, double containerH = 0)
    {
        var state = GardenElementState.From(snapshot.StateTypeName);
        var house = new GreenhouseFromViewModel
        {
            Id = snapshot.Properties.Id, ContainerCanvasWidth = containerW, ContainerCanvasHeight = containerH,
            Rotation = snapshot.Rotation, IsLocked = snapshot.IsLocked, StateTypeName = snapshot.StateTypeName,
            StateDisplayName = state.DisplayName, StateColor = state.StatusColor
        };
        GreenhouseEditorMapper.Apply(snapshot.Properties, house);
        CanvasGeometryMapper.Apply(snapshot.Rectangle, house);
        house.InnerElements = new(snapshot.Elements.Select(e => MapElement(e, house.InnerCanvasWidth, house.InnerCanvasHeight)));
        foreach (var element in house.InnerElements) { element.ContainerElements = house.InnerElements; element.ContainerGreenhouses = []; }
        return house;
    }
}
