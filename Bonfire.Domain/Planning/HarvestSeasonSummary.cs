using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public sealed record HarvestSeasonSummary(
    decimal PerPlantKg,
    decimal PerM2Kg,
    int UnknownPerPlantCount,
    int UnknownPerM2Count,
    int BatchCount)
{
    public static Result<HarvestSeasonSummary> Calculate(
        IEnumerable<(HarvestPlanningInput Input, int TargetPlants)> batches)
    {
        var result = Result.Success(new HarvestSeasonSummary(0, 0, 0, 0, 0));
        foreach (var batch in batches)
        {
            result = result.Bind(summary =>
                HarvestProjection.Calculate(batch.Input, batch.TargetPlants).Bind(summary.Add));
            if (result.IsFailure) return result;
        }

        return result;
    }

    private Result<HarvestSeasonSummary> Add(HarvestProjection forecast)
    {
        if (BatchCount == int.MaxValue)
            return Result.Failure<HarvestSeasonSummary>("Число партий выходит за допустимый диапазон");
        return DecimalArithmetic.Add(PerPlantKg, forecast.PerPlantKg.Match(x => x, () => 0m)).Bind(plants =>
                DecimalArithmetic.Add(PerM2Kg, forecast.PerM2Kg.Match(x => x, () => 0m)).Bind(area =>
                    Result.Success(new HarvestSeasonSummary(plants, area,
                        UnknownPerPlantCount + (forecast.PerPlantKg is Option<decimal>.None ? 1 : 0),
                        UnknownPerM2Count + (forecast.PerM2Kg is Option<decimal>.None ? 1 : 0), BatchCount + 1))))
            .Match(Result.Success,
                _ => Result.Failure<HarvestSeasonSummary>("Сезонный прогноз превышает допустимый диапазон"));
    }
}