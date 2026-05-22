using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using BonfireDB.Entities.GardenPlanning.States;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.Services;

internal class GardenService(
    IRepository<GardenPlan> plans,
    IRepository<Garden> gardens,
    IRepository<Greenhouse> greenhouses,
    IRepository<GardenElement> elements,
    IRepository<PlantingSpot> spots)
    : IGardenService
{
    // --- Чтение ---

    public async Task<IReadOnlyList<GardenPlan>> GetPlansOrderedByYearDescAsync()
        => await plans.Items.OrderByDescending(p => p.Year).ToListAsync();

    public async Task<IReadOnlyList<Garden>> GetGardensByPlanAsync(int planId)
        => await gardens.Items.Where(g => g.GardenPlanId == planId).ToListAsync();

    public async Task<Garden?> GetGardenByIdAsync(int id)
        => await gardens.Items.FirstOrDefaultAsync(g => g.Id == id);

    public async Task<GardenElement?> GetElementByIdAsync(int id)
        => await elements.Items.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Greenhouse?> GetGreenhouseByIdAsync(int id)
        => await greenhouses.Items.FirstOrDefaultAsync(g => g.Id == id);

    // --- Планы ---

    public async Task<GardenPlan> CreatePlanAsync(string name, int year, string? description = null)
    {
        var plan = new GardenPlan { Name = name, Year = year, Description = description };
        return await plans.AddAsync(plan);
    }

    public async Task<GardenPlan> UpdatePlanAsync(GardenPlan plan)
    {
        await plans.UpdateAsync(plan);
        return plan;
    }

    public async Task DeletePlanAsync(GardenPlan plan) =>
        await plans.RemoveAsync(plan.Id);

    // --- Участки ---

    public async Task<Garden> CreateGardenAsync(int planId, string name,
        double widthMeters, double heightMeters, double scale = 40)
    {
        var garden = new Garden
        {
            GardenPlanId = planId,
            Name = name,
            WidthMeters = widthMeters,
            HeightMeters = heightMeters,
            CanvasWidth = widthMeters * scale,
            CanvasHeight = heightMeters * scale
        };
        return await gardens.AddAsync(garden);
    }

    public async Task<Garden> UpdateGardenAsync(Garden garden)
    {
        await gardens.UpdateAsync(garden);
        return garden;
    }

    public async Task DeleteGardenAsync(Garden garden) =>
        await gardens.RemoveAsync(garden.Id);

    // --- Теплицы ---

    public async Task<Greenhouse> AddGreenhouseAsync(int parentPlotId, string name,
        double widthMeters, double heightMeters, double scale = 40)
    {
        var gh = new Greenhouse
        {
            ParentPlotId = parentPlotId,
            Name = name,
            WidthMeters = widthMeters,
            HeightMeters = heightMeters,
            CanvasWidth = widthMeters * scale,
            CanvasHeight = heightMeters * scale,
            DisplayWidth = widthMeters * scale,
            DisplayHeight = heightMeters * scale
        };
        return await greenhouses.AddAsync(gh);
    }

    public async Task<Greenhouse> UpdateGreenhouseAsync(Greenhouse greenhouse)
    {
        await greenhouses.UpdateAsync(greenhouse);
        return greenhouse;
    }

    public async Task DeleteGreenhouseAsync(Greenhouse greenhouse) =>
        await greenhouses.RemoveAsync(greenhouse.Id);

    // --- Элементы ---

    public async Task<TElement> AddElementAsync<TElement>(TElement element)
        where TElement : GardenElement
    {
        return (TElement)await elements.AddAsync(element);
    }

    public async Task<GardenElement> UpdateElementAsync(GardenElement element)
    {
        await elements.UpdateAsync(element);
        return element;
    }

    public async Task DeleteElementAsync(GardenElement element) =>
        await elements.RemoveAsync(element.Id);

    // --- Переходы состояний ---

    public async Task ChangeElementStateAsync(GardenElement element, GardenElementState newState)
    {
        element.TransitionTo(newState);
        await elements.UpdateAsync(element);
    }

    public async Task ChangeGreenhouseStateAsync(Greenhouse greenhouse, GardenElementState newState)
    {
        greenhouse.TransitionTo(newState);
        await greenhouses.UpdateAsync(greenhouse);
    }

    // --- Посадки ---

    public async Task<PlantingSpot?> GetSpotAsync(int spotId)
        => await gardens.Items
            .SelectMany(g => g.Elements)
            .SelectMany(e => e.PlantingSpots)
            .FirstOrDefaultAsync(s => s.Id == spotId);

    public async Task<PlantingSpot> PlantSeedlingAsync(int elementId, int row, int col,
        SeedlingInfo seedlingInfo, DateTime plantedDate)
    {
        var spot = new PlantingSpot
        {
            GardenElementId = elementId,
            Row = row,
            Column = col,
            SeedlingInfoId = seedlingInfo.Id,
            PlantedDate = plantedDate
        };
        spot.TransitionTo(new PlantedSpotState());
        return await spots.AddAsync(spot);
    }

    public async Task ClearSpotAsync(PlantingSpot spot)
    {
        spot.SeedlingInfoId = null;
        spot.PlantedDate = null;
        spot.HarvestDate = null;
        spot.StateTypeName = nameof(EmptySpotState);
        await spots.UpdateAsync(spot);
    }

    public async Task<PlantingSpot> UpdateSpotAsync(PlantingSpot spot)
    {
        await spots.UpdateAsync(spot);
        return spot;
    }

    public async Task ChangeSpotStateAsync(PlantingSpot spot, PlantingSpotState newState,
        string? plantLabel = null, DateTime? plantedDate = null, int? seedlingInfoId = null)
    {
        spot.TransitionTo(newState);
        if (plantLabel is not null)     spot.Note          = plantLabel;
        if (plantedDate.HasValue)       spot.PlantedDate   = plantedDate;
        if (seedlingInfoId.HasValue)    spot.SeedlingInfoId = seedlingInfoId;
        await spots.UpdateAsync(spot);
    }

    // --- Перестройка сетки ---

    public async Task RebuildGridAsync(GardenElement element, int rows, int cols)
    {
        if (!element.State.CanModifyGrid)
            throw new InvalidOperationException(
                $"В состоянии «{element.State.DisplayName}» изменение сетки запрещено");

        // Удалить ячейки, выходящие за новые границы
        var toRemove = element.PlantingSpots
            .Where(s => s.Row >= rows || s.Column >= cols)
            .ToList();
        foreach (var spot in toRemove)
        {
            element.PlantingSpots.Remove(spot);
            await spots.RemoveAsync(spot.Id);
        }

        // Создать недостающие ячейки (те, которых ещё нет в сетке)
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                if (!element.PlantingSpots.Any(s => s.Row == r && s.Column == c))
                {
                    var newSpot = new PlantingSpot
                    {
                        GardenElementId = element.Id,
                        Row = r,
                        Column = c
                    };
                    await spots.AddAsync(newSpot);
                    // EF auto-fixup уже добавил newSpot в element.PlantingSpots
                    // (через relationship fixup при db.Entry().State = Added)
                }
            }
        }

        element.GridRows = rows;
        element.GridColumns = cols;
        await elements.UpdateAsync(element);
    }
}
