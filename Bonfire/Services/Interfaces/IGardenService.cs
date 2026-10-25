using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.SpotStates;
using Bonfire.Domain.Garden.States;

namespace Bonfire.Services.Interfaces;

public interface IGardenService
{
    Task<Result<GeometryResultFromViewModel>> SaveGeometryAsync(SaveGardenGeometryFromViewModel request);

    Task<Result<ElementPropertiesResultFromViewModel>> EditElementPropertiesAsync(EditElementPropertiesFromViewModel request);

    Task<Result<GreenhousePropertiesResultFromViewModel>> EditGreenhousePropertiesAsync(EditGreenhousePropertiesFromViewModel request);

    Task<Result<GardenPropertiesResultFromViewModel>> EditGardenPropertiesAsync(EditGardenPropertiesFromViewModel request);

    Task<Result<GardenLockResultFromViewModel>> SetLockAsync(SetGardenLockFromViewModel request);

    Task<Result<ElementDeletionResultFromViewModel>> DeleteGardenElementAsync(DeleteGardenElementFromViewModel request);

    Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteGreenhouseByIdAsync(DeleteGreenhouseFromViewModel request);

    // --- Чтение ---
    Task<IReadOnlyList<GardenPlan>> GetPlansOrderedByYearDescAsync();
    Task<IReadOnlyList<Garden>> GetGardensByPlanAsync(int planId);
    Task<Garden?> GetGardenByIdAsync(int id);
    Task<GardenElement?> GetElementByIdAsync(int id);
    Task<Greenhouse?> GetGreenhouseByIdAsync(int id);

    // --- Планы ---
    Task<Result<ElementCreationResultFromViewModel>> CreateElementFromRequestAsync(CreateGardenElementFromViewModel request);
    Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseFromRequestAsync(CreateGreenhouseFromViewModel request);
    Task<Result<GardenPlanResultFromViewModel>> CreateCurrentPlanAsync();
    Task<Result<GardenPropertiesResultFromViewModel>> CreateGardenFromRequestAsync(CreateGardenFromViewModel request);

    Task<GardenPlan> CreatePlanAsync(string name, int year, string? description = null);
    Task<GardenPlan> UpdatePlanAsync(GardenPlan plan);
    Task DeletePlanAsync(GardenPlan plan);
    Task<Result<GardenPlanDeletionResultFromViewModel>> DeletePlanByIdAsync(DeleteGardenPlanFromViewModel request);

    // --- Участки огорода ---
    Task<Garden> CreateGardenAsync(int planId, string name,
        double widthMeters, double heightMeters, double scale = 150);
    Task<Garden> UpdateGardenAsync(Garden garden);
    Task DeleteGardenAsync(Garden garden);
    Task<Result<GardenDeletionResultFromViewModel>> DeleteGardenByIdAsync(DeleteGardenFromViewModel request);

    // --- Теплицы (наследники GardenPlot) ---
    Task<Greenhouse> AddGreenhouseAsync(int parentPlotId, string name,
        double widthMeters, double heightMeters, double scale = 150,
        double x = 0, double y = 0);
    Task<Greenhouse> UpdateGreenhouseAsync(Greenhouse greenhouse);
    Task DeleteGreenhouseAsync(Greenhouse greenhouse);

    // --- Элементы (грядки, парники, цветники, открытый грунт) ---
    Task<TElement> AddElementAsync<TElement>(TElement element)
        where TElement : GardenElement;
    Task<GardenElement> UpdateElementAsync(GardenElement element);
    Task DeleteElementAsync(GardenElement element);

    // --- Переходы состояний ---
    Task<Result<GardenStateResultFromViewModel>> ChangeStateAsync(ChangeGardenStateFromViewModel request, CancellationToken cancellationToken = default);
    Task ChangeElementStateAsync(GardenElement element, GardenElementState newState);
    Task ChangeGreenhouseStateAsync(Greenhouse greenhouse, GardenElementState newState);

    // --- Посадки в ячейки ---
    /// <summary>Загружает ячейку по Id (по элементам участков верхнего уровня).</summary>
    Task<PlantingSpot?> GetSpotAsync(int spotId);
    Task<PlantingSpot> PlantSeedlingAsync(int elementId, int row, int col,
        SeedlingInfo seedlingInfo, DateTime plantedDate);
    Task ClearSpotAsync(PlantingSpot spot);
    Task<PlantingSpot> UpdateSpotAsync(PlantingSpot spot);

    /// <summary>Переводит ячейку в новое состояние и сохраняет в БД.</summary>
    Task ChangeSpotStateAsync(PlantingSpot spot, PlantingSpotState newState,
        string? plantLabel = null, DateTime? plantedDate = null, int? seedlingInfoId = null);

    // --- Перестройка сетки ---
    Task<Result<GridResultFromViewModel>> ResizeGridAsync(ResizeGridFromViewModel request, CancellationToken cancellationToken = default);
    Task RebuildGridAsync(GardenElement element, int rows, int cols);
}
