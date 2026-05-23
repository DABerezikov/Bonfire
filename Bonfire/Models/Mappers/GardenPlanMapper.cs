using System.Collections.ObjectModel;
using System.Linq;
using BonfireDB.Entities.GardenPlanning;

namespace Bonfire.Models.Mappers;

/// <summary>
/// Маппинг сущностей планировщика огорода в DTO для UI.
/// Чистые функции без зависимостей от ViewModel — легко тестируются.
/// </summary>
internal static class GardenPlanMapper
{
    public static GardenFromViewModel MapGarden(Garden g)
    {
        var elements = new ObservableCollection<GardenElementFromViewModel>(
            g.Elements.Select(e => MapElement(e, g.CanvasWidth, g.CanvasHeight)));

        var greenhouses = new ObservableCollection<GreenhouseFromViewModel>(
            g.Greenhouses.Select(gh => MapGreenhouse(gh, g.CanvasWidth, g.CanvasHeight)));

        // Каждый элемент получает ссылки на коллекции братских объектов
        // для проверки коллизий в code-behind во время drag/resize.
        foreach (var el in elements)
        {
            el.ContainerElements   = elements;
            el.ContainerGreenhouses = greenhouses;
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
