using System.Collections.ObjectModel;
using System.Linq;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.States;
using BonfireDB.Entities.GardenPlanning;

namespace Bonfire.Models.Mappers;

/// <summary>
/// Маппинг сущностей планировщика огорода в DTO для UI.
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

    public static GardenPlan MapPlan(GardenPlanResultFromViewModel plan) => new()
    {
        Id = plan.Id, Name = plan.Name, Year = plan.Year, Description = plan.Description.ToNullable()
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

    public static GardenFromViewModel MapGarden(Garden g)
    {
        var elements = new ObservableCollection<GardenElementFromViewModel>(
            g.Elements.Select(e => MapElement(e, g.CanvasWidth, g.CanvasHeight)));

        var greenhouses = new ObservableCollection<GreenhouseFromViewModel>(
            g.Greenhouses.Select(gh => MapGreenhouse(gh, g.CanvasWidth, g.CanvasHeight)));

        // Каждый объект получает ссылки на коллекции братских объектов
        // для проверки коллизий в code-behind во время drag/resize.
        foreach (var el in elements)
        {
            el.ContainerElements    = elements;
            el.ContainerGreenhouses = greenhouses;
        }
        foreach (var gh in greenhouses)
        {
            gh.ContainerElements    = elements;
            gh.ContainerGreenhouses = greenhouses;
        }

        return new GardenFromViewModel
        {
            Id = g.Id,
            Name = g.Name,
            WidthMeters = g.WidthMeters,
            HeightMeters = g.HeightMeters,
            CanvasWidth = g.CanvasWidth,
            CanvasHeight = g.CanvasHeight,
            Address = g.Address,
            Note = g.Note,
            Elements = elements,
            Greenhouses = greenhouses
        };
    }

    public static GardenElementFromViewModel MapElement(GardenElement e,
        double containerW = 0, double containerH = 0)
    {
        var state = e.State;
        return new GardenElementFromViewModel
        {
            Id = e.Id,
            Name = e.Name,
            ElementType = e.GetType().Name,
            ContainerCanvasWidth  = containerW,
            ContainerCanvasHeight = containerH,
            X = e.X,
            Y = e.Y,
            Width = e.DisplayWidth,
            Height = e.DisplayHeight,
            Rotation = e.Rotation,
            IsLocked = e.IsLocked,
            StateTypeName = e.StateTypeName,
            StateDisplayName = state.DisplayName,
            StateColor = state.StatusColor,
            CanAddPlanting = state.CanAddPlanting,
            CanModifyGrid = state.CanModifyGrid,
            GridRows = e.GridRows,
            GridColumns = e.GridColumns,
            SoilType = e.SoilType,
            Note = e.Note,
            Orientation = (e as Bed)?.Orientation,
            CoverMaterial = (e as ColdFrame)?.CoverMaterial,
            Shape = (e as FlowerBed)?.Shape,
            PlantingSpots = new ObservableCollection<PlantingSpotFromViewModel>(
                e.PlantingSpots.OrderBy(s => s.Row).ThenBy(s => s.Column).Select(MapSpot))
        };
    }

    public static GreenhouseFromViewModel MapGreenhouse(Greenhouse gh,
        double containerW = 0, double containerH = 0)
    {
        var state = gh.State;
        var innerElements = new ObservableCollection<GardenElementFromViewModel>(
            gh.Elements.Select(e => MapElement(e, gh.CanvasWidth, gh.CanvasHeight)));

        // Проставляем ссылки для проверки коллизий при drag/resize (как в MapGarden)
        foreach (var el in innerElements)
        {
            el.ContainerElements    = innerElements;
            el.ContainerGreenhouses = [];
        }

        return new GreenhouseFromViewModel
        {
            Id = gh.Id,
            Name = gh.Name,
            ContainerCanvasWidth  = containerW,
            ContainerCanvasHeight = containerH,
            X = gh.X,
            Y = gh.Y,
            DisplayWidth = gh.DisplayWidth,
            DisplayHeight = gh.DisplayHeight,
            Rotation = gh.Rotation,
            WidthMeters = gh.WidthMeters,
            HeightMeters = gh.HeightMeters,
            InnerCanvasWidth = gh.CanvasWidth,
            InnerCanvasHeight = gh.CanvasHeight,
            IsLocked = gh.IsLocked,
            StateTypeName = gh.StateTypeName,
            StateDisplayName = state.DisplayName,
            StateColor = state.StatusColor,
            Material = gh.Material,
            Note = gh.Note,
            InnerElements = innerElements
        };
    }

    public static PlantingSpotFromViewModel MapSpot(PlantingSpot s)
        => new()
        {
            Id           = s.Id,
            Row          = s.Row,
            Column       = s.Column,
            StateTypeName = s.StateTypeName,   // computed props (CellColor, CanGoTo* …) рассчитаются сами
            SeedlingInfoId = s.SeedlingInfoId,
            PlantedDate  = s.PlantedDate,
            PlantLabel   = s.Note,             // Note хранит «что посажено»
            Note         = s.Note
        };
}
