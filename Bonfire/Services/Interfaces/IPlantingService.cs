using System;
using System.Threading.Tasks;
using Bonfire.Models;

namespace Bonfire.Services.Interfaces;

/// <summary>Параметры посадки в ячейку сетки.</summary>
/// <param name="SpotId">Id ячейки <c>PlantingSpot</c>.</param>
/// <param name="Kind">Источник материала: рассада или семена.</param>
/// <param name="EntityId">Id рассады (<c>Seedling</c>) или пакета семян (<c>Seed</c>).</param>
/// <param name="IsWeightBased">True — списываем граммы, False — штуки.</param>
/// <param name="Amount">Сколько списать (в штуках или граммах).</param>
/// <param name="PlantedDate">Дата посадки.</param>
/// <param name="PlantName">«Культура Сорт» — метка ячейки.</param>
/// <param name="PlantPlace">Полный адрес посадки «План / Участок / Элемент».</param>
public sealed record PlantRequest(
    int SpotId,
    PlantSourceKind Kind,
    int EntityId,
    bool IsWeightBased,
    double Amount,
    DateTime PlantedDate,
    string PlantName,
    string? PlantPlace);

/// <summary>Результат посадки.</summary>
/// <param name="Success">False — посадка не выполнена (ячейка не найдена,
/// недопустимый переход состояния или источник не найден).</param>
/// <param name="SeedlingInfoId">Id созданной/привязанной записи о рассаде.</param>
public sealed record PlantResult(bool Success, int? SeedlingInfoId);

/// <summary>
/// Бизнес-операция «посадить в ячейку»: списание инвентаря (рассада/семена),
/// создание записи о рассаде и перевод ячейки в состояние «Посажено».
/// </summary>
public interface IPlantingService
{
    Task<PlantResult> PlantAsync(PlantRequest request);
}
