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

internal class GardenService(IUnitOfWorkFactory uowFactory) : IGardenService
{
    // --- Чтение ---

    public async Task<IReadOnlyList<GardenPlan>> GetPlansOrderedByYearDescAsync()
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<GardenPlan>().Items.OrderByDescending(p => p.Year).ToListAsync();
    }

    public async Task<IReadOnlyList<Garden>> GetGardensByPlanAsync(int planId)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Garden>().Items.Where(g => g.GardenPlanId == planId).ToListAsync();
    }

    public async Task<Garden?> GetGardenByIdAsync(int id)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Garden>().Items.FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<GardenElement?> GetElementByIdAsync(int id)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<GardenElement>().Items.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Greenhouse?> GetGreenhouseByIdAsync(int id)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Greenhouse>().Items.FirstOrDefaultAsync(g => g.Id == id);
    }

    // --- Планы ---

    public async Task<GardenPlan> CreatePlanAsync(string name, int year, string? description = null)
    {
        await using var uow = uowFactory.Create();
        var plan = new GardenPlan { Name = name, Year = year, Description = description };
        await uow.Repository<GardenPlan>().AddAsync(plan);
        await uow.SaveChangesAsync();
        return plan;
    }

    public async Task<GardenPlan> UpdatePlanAsync(GardenPlan plan)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenPlan>().UpdateAsync(plan);
        await uow.SaveChangesAsync();
        return plan;
    }

    public async Task DeletePlanAsync(GardenPlan plan)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenPlan>().RemoveAsync(plan.Id);
        await uow.SaveChangesAsync();
    }

    // --- Участки ---

    public async Task<Garden> CreateGardenAsync(int planId, string name,
        double widthMeters, double heightMeters, double scale = 150)
    {
        await using var uow = uowFactory.Create();
        var garden = new Garden
        {
            GardenPlanId = planId,
            Name = name,
            WidthMeters = widthMeters,
            HeightMeters = heightMeters,
            CanvasWidth = widthMeters * scale,
            CanvasHeight = heightMeters * scale
        };
        await uow.Repository<Garden>().AddAsync(garden);
        await uow.SaveChangesAsync();
        return garden;
    }

    public async Task<Garden> UpdateGardenAsync(Garden garden)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Garden>().UpdateAsync(garden);
        await uow.SaveChangesAsync();
        return garden;
    }

    public async Task DeleteGardenAsync(Garden garden)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Garden>().RemoveAsync(garden.Id);
        await uow.SaveChangesAsync();
    }

    // --- Теплицы ---

    public async Task<Greenhouse> AddGreenhouseAsync(int parentPlotId, string name,
        double widthMeters, double heightMeters, double scale = 150,
        double x = 0, double y = 0)
    {
        await using var uow = uowFactory.Create();
        var gh = new Greenhouse
        {
            ParentPlotId = parentPlotId,
            Name = name,
            WidthMeters = widthMeters,
            HeightMeters = heightMeters,
            CanvasWidth = widthMeters * scale,
            CanvasHeight = heightMeters * scale,
            DisplayWidth = widthMeters * scale,
            DisplayHeight = heightMeters * scale,
            X = x,
            Y = y
        };
        await uow.Repository<Greenhouse>().AddAsync(gh);
        await uow.SaveChangesAsync();
        return gh;
    }

    public async Task<Greenhouse> UpdateGreenhouseAsync(Greenhouse greenhouse)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Greenhouse>().UpdateAsync(greenhouse);
        await uow.SaveChangesAsync();
        return greenhouse;
    }

    public async Task DeleteGreenhouseAsync(Greenhouse greenhouse)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Greenhouse>().RemoveAsync(greenhouse.Id);
        await uow.SaveChangesAsync();
    }

    // --- Элементы ---

    public async Task<TElement> AddElementAsync<TElement>(TElement element)
        where TElement : GardenElement
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenElement>().AddAsync(element);
        await uow.SaveChangesAsync();
        return element;
    }

    public async Task<GardenElement> UpdateElementAsync(GardenElement element)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenElement>().UpdateAsync(element);
        await uow.SaveChangesAsync();
        return element;
    }

    public async Task DeleteElementAsync(GardenElement element)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenElement>().RemoveAsync(element.Id);
        await uow.SaveChangesAsync();
    }

    // --- Переходы состояний ---

    public async Task ChangeElementStateAsync(GardenElement element, GardenElementState newState)
    {
        element.TransitionTo(newState);
        await using var uow = uowFactory.Create();
        await uow.Repository<GardenElement>().UpdateAsync(element);
        await uow.SaveChangesAsync();
    }

    public async Task ChangeGreenhouseStateAsync(Greenhouse greenhouse, GardenElementState newState)
    {
        greenhouse.TransitionTo(newState);
        await using var uow = uowFactory.Create();
        await uow.Repository<Greenhouse>().UpdateAsync(greenhouse);
        await uow.SaveChangesAsync();
    }

    // --- Посадки ---

    public async Task<PlantingSpot?> GetSpotAsync(int spotId)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<PlantingSpot>().Items
            .FirstOrDefaultAsync(s => s.Id == spotId);
    }

    public async Task<PlantingSpot> PlantSeedlingAsync(int elementId, int row, int col,
        SeedlingInfo seedlingInfo, DateTime plantedDate)
    {
        await using var uow = uowFactory.Create();
        var spot = new PlantingSpot
        {
            GardenElementId = elementId,
            Row = row,
            Column = col,
            SeedlingInfoId = seedlingInfo.Id,
            PlantedDate = plantedDate
        };
        spot.TransitionTo(new PlantedSpotState());
        await uow.Repository<PlantingSpot>().AddAsync(spot);
        await uow.SaveChangesAsync();
        return spot;
    }

    public async Task ClearSpotAsync(PlantingSpot spot)
    {
        spot.SeedlingInfoId = null;
        spot.PlantedDate = null;
        spot.HarvestDate = null;
        spot.StateTypeName = nameof(EmptySpotState);
        await using var uow = uowFactory.Create();
        await uow.Repository<PlantingSpot>().UpdateAsync(spot);
        await uow.SaveChangesAsync();
    }

    public async Task<PlantingSpot> UpdateSpotAsync(PlantingSpot spot)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<PlantingSpot>().UpdateAsync(spot);
        await uow.SaveChangesAsync();
        return spot;
    }

    public async Task ChangeSpotStateAsync(PlantingSpot spot, PlantingSpotState newState,
        string? plantLabel = null, DateTime? plantedDate = null, int? seedlingInfoId = null)
    {
        spot.TransitionTo(newState);
        if (plantLabel is not null)     spot.Note          = plantLabel;
        if (plantedDate.HasValue)       spot.PlantedDate   = plantedDate;
        if (seedlingInfoId.HasValue)    spot.SeedlingInfoId = seedlingInfoId;
        await using var uow = uowFactory.Create();
        await uow.Repository<PlantingSpot>().UpdateAsync(spot);
        await uow.SaveChangesAsync();
    }

    // --- Перестройка сетки ---

    public async Task RebuildGridAsync(GardenElement element, int rows, int cols)
    {
        if (!element.State.CanModifyGrid)
            throw new InvalidOperationException(
                $"В состоянии «{element.State.DisplayName}» изменение сетки запрещено");

        await using var uow = uowFactory.Create();
        var spots = uow.Repository<PlantingSpot>();

        // Удалить ячейки, выходящие за новые границы
        var toRemove = element.PlantingSpots
            .Where(s => s.Row >= rows || s.Column >= cols)
            .ToList();
        foreach (var spot in toRemove)
        {
            element.PlantingSpots.Remove(spot);
            await spots.RemoveAsync(spot.Id);
        }

        // Создать недостающие ячейки (FK GardenElementId задаётся явно)
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
                    element.PlantingSpots.Add(newSpot);
                    await spots.AddAsync(newSpot);
                }
            }
        }

        element.GridRows = rows;
        element.GridColumns = cols;
        await uow.Repository<GardenElement>().UpdateAsync(element);
        await uow.SaveChangesAsync();
    }
}
