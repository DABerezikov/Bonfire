using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public sealed record DayRange
{
    public int Min { get; }
    public int Max { get; }
    private DayRange(int min, int max) => (Min, Max) = (min, max);
    public static Result<DayRange> Create(int min, int max) => min >= 0 && max >= min
        ? Result.Success(new DayRange(min, max))
        : Result.Failure<DayRange>("Длительность должна быть неотрицательной, минимум не больше максимума");
}

public sealed record GerminationPercentage
{
    public decimal Value { get; }
    private GerminationPercentage(decimal value) => Value = value;
    public static Result<GerminationPercentage> Create(decimal value) => value > 0 && value <= 100
        ? Result.Success(new GerminationPercentage(value))
        : Result.Failure<GerminationPercentage>("Всхожесть должна быть больше 0 и не больше 100%");
}

public sealed record LossPercentage
{
    public decimal Value { get; }
    private LossPercentage(decimal value) => Value = value;
    public static Result<LossPercentage> Create(decimal value) => value >= 0 && value < 100
        ? Result.Success(new LossPercentage(value))
        : Result.Failure<LossPercentage>("Потери после всходов должны быть от 0 до 100% исключительно");
}

public sealed record PlantCount
{
    public int Value { get; }
    private PlantCount(int value) => Value = value;
    public static Result<PlantCount> Create(int value) => value > 0
        ? Result.Success(new PlantCount(value))
        : Result.Failure<PlantCount>("Количество должно быть положительным целым числом");
}

public sealed record RegionId
{
    public int Value { get; }
    private RegionId(int value) => Value = value;
    public static Result<RegionId> Create(int value) => value > 0
        ? Result.Success(new RegionId(value))
        : Result.Failure<RegionId>("Некорректный идентификатор региона");
}

public sealed record SeasonalPlantingPlanId
{
    public int Value { get; }
    private SeasonalPlantingPlanId(int value) => Value = value;
    public static Result<SeasonalPlantingPlanId> Create(int value) => value > 0
        ? Result.Success(new SeasonalPlantingPlanId(value))
        : Result.Failure<SeasonalPlantingPlanId>("Некорректный идентификатор сезонного плана");
}

public sealed record PlannedBatchId
{
    public int Value { get; }
    private PlannedBatchId(int value) => Value = value;
    public static Result<PlannedBatchId> Create(int value) => value > 0
        ? Result.Success(new PlannedBatchId(value))
        : Result.Failure<PlannedBatchId>("Некорректный идентификатор планируемой партии");
}
