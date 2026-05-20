using System;
using System.Linq;
using System.Threading.Tasks;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using BonfireDB.Entities.GardenPlanning.States;

namespace Bonfire.Services.Interfaces;

public interface IGardenService
{
    IQueryable<GardenPlan> Plans { get; }
    IQueryable<Garden> Gardens { get; }

    // --- Планы ---
    Task<GardenPlan> CreatePlanAsync(string name, int year, string? description = null);
    Task<GardenPlan> UpdatePlanAsync(GardenPlan plan);
    Task DeletePlanAsync(GardenPlan plan);

    // --- Участки огорода ---
    Task<Garden> CreateGardenAsync(int planId, string name,
        double widthMeters, double heightMeters, double scale = 40);
    Task<Garden> UpdateGardenAsync(Garden garden);
    Task DeleteGardenAsync(Garden garden);

    // --- Теплицы (наследники GardenPlot) ---
    Task<Greenhouse> AddGreenhouseAsync(int parentPlotId, string name,
        double widthMeters, double heightMeters, double scale = 40);
    Task<Greenhouse> UpdateGreenhouseAsync(Greenhouse greenhouse);
    Task DeleteGreenhouseAsync(Greenhouse greenhouse);

    // --- Элементы (грядки, парники, цветники, открытый грунт) ---
    Task<TElement> AddElementAsync<TElement>(TElement element)
        where TElement : GardenElement;
    Task<GardenElement> UpdateElementAsync(GardenElement element);
    Task DeleteElementAsync(GardenElement element);

    // --- Переходы состояний ---
    Task ChangeElementStateAsync(GardenElement element, GardenElementState newState);
    Task ChangeGreenhouseStateAsync(Greenhouse greenhouse, GardenElementState newState);

    // --- Посадки в ячейки ---
    Task<PlantingSpot> PlantSeedlingAsync(int elementId, int row, int col,
        SeedlingInfo seedlingInfo, DateTime plantedDate);
    Task ClearSpotAsync(PlantingSpot spot);
    Task<PlantingSpot> UpdateSpotAsync(PlantingSpot spot);

    /// <summary>Переводит ячейку в новое состояние и сохраняет в БД.</summary>
    Task ChangeSpotStateAsync(PlantingSpot spot, PlantingSpotState newState,
        string? plantLabel = null, DateTime? plantedDate = null, int? seedlingInfoId = null);

    // --- Перестройка сетки ---
    Task RebuildGridAsync(GardenElement element, int rows, int cols);
}
