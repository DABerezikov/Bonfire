using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

/// <summary>Метры; отступы заданы относительно направления рядов. Снимок, не резерв грядки.</summary>
public sealed record BedLayout(decimal Length, decimal Width, decimal PlantStep, decimal RowStep,
    decimal StartMargin, decimal EndMargin, decimal LeftMargin, decimal RightMargin, bool RowsAcross);

public sealed record BedCapacity(int Rows, int PerRow, int Plants, decimal AreaM2, decimal TheoreticalDensity)
{
    public static Result<BedCapacity> Calculate(BedLayout layout)
    {
        if (layout.Length <= 0 || layout.Width <= 0 || layout.PlantStep <= 0 || layout.RowStep <= 0 ||
            layout.StartMargin < 0 || layout.EndMargin < 0 || layout.LeftMargin < 0 || layout.RightMargin < 0)
            return Result.Failure<BedCapacity>("Размеры и шаги должны быть положительными, отступы — неотрицательными");
        try
        {
            int Count(decimal size, decimal before, decimal after, decimal step)
                => size - before - after < 0 ? 0 : checked((int)decimal.Floor((size - before - after) / step) + 1);
            var along = layout.RowsAcross ? layout.Width : layout.Length;
            var across = layout.RowsAcross ? layout.Length : layout.Width;
            var perRow = Count(along, layout.StartMargin, layout.EndMargin, layout.PlantStep);
            var rows = Count(across, layout.LeftMargin, layout.RightMargin, layout.RowStep);
            return Result.Success(new BedCapacity(rows, perRow, checked(rows * perRow), checked(layout.Length * layout.Width),
                1m / checked(layout.PlantStep * layout.RowStep)));
        }
        catch (Exception error) when (error is OverflowException or DivideByZeroException)
        { return Result.Failure<BedCapacity>("Размеры или плотность выходят за допустимый диапазон"); }
    }
}

public enum HarvestPlanningMode { ManualPlants, BedCapacity, TargetPerPlant, TargetPerM2 }

public sealed record HarvestPlanningInput
{
    public HarvestPlanningMode Mode { get; init; }
    public Option<BedLayout> Layout { get; init; } = Option.None<BedLayout>();
    public Option<int> BedId { get; init; } = Option.None<int>();
    public Option<string> BedName { get; init; } = Option.None<string>();
    public Option<decimal> PerPlantKg { get; init; } = Option.None<decimal>();
    public Option<decimal> PerM2Kg { get; init; } = Option.None<decimal>();
    public ParameterSource PerPlantSource { get; init; } = ParameterSource.Batch;
    public ParameterSource PerM2Source { get; init; } = ParameterSource.Batch;
    public Option<decimal> YieldAreaM2 { get; init; } = Option.None<decimal>();
    public Option<decimal> TargetKg { get; init; } = Option.None<decimal>();
}

public sealed record HarvestProjection(int TargetPlants, Option<BedCapacity> Capacity,
    Option<decimal> PerPlantKg, Option<decimal> PerM2Kg, Option<decimal> RequiredAreaM2,
    Option<int> PlantDeficit, Option<decimal> AreaDeficitM2, bool UsesDensityEstimate)
{
    public static Result<HarvestProjection> Calculate(HarvestPlanningInput input, int manualPlants)
    {
        if (!Enum.IsDefined(input.Mode) || !Enum.IsDefined(input.PerPlantSource) || !Enum.IsDefined(input.PerM2Source) ||
            input.PerPlantKg.Match(x => x < 0, () => false) || input.PerM2Kg.Match(x => x < 0, () => false) ||
            input.YieldAreaM2.Match(x => x < 0, () => false) || input.TargetKg.Match(x => x <= 0, () => false) ||
            input.BedId.Match(x => x <= 0, () => false))
            return Result.Failure<HarvestProjection>("Неверная норма, площадь, цель урожая или режим расчёта");
        return input.Layout.Match(x => BedCapacity.Calculate(x).Bind(value => Result.Success(Option.Some(value))), () => Result.Success(Option.None<BedCapacity>()))
            .Bind(capacity => Build(input, manualPlants, capacity));
    }

    private static Result<HarvestProjection> Build(HarvestPlanningInput input, int manualPlants, Option<BedCapacity> capacity)
    {
        try
        {
            var plants = manualPlants;
            var requiredArea = Option.None<decimal>();
            if (input.Mode == HarvestPlanningMode.BedCapacity)
            {
                if (capacity is not Option<BedCapacity>.Some bed) return Failure("Задайте размеры и схему грядки");
                plants = bed.Value.Plants;
            }
            if (input.Mode is HarvestPlanningMode.TargetPerPlant or HarvestPlanningMode.TargetPerM2)
            {
                if (input.TargetKg is not Option<decimal>.Some target) return Failure("Задайте желаемый урожай в кг");
                var selected = input.Mode == HarvestPlanningMode.TargetPerPlant ? input.PerPlantKg : input.PerM2Kg;
                if (selected is not Option<decimal>.Some rate || rate.Value <= 0) return Failure("Для цели нужна положительная выбранная норма");
                var perPlant = rate.Value;
                if (input.Mode == HarvestPlanningMode.TargetPerM2)
                {
                    if (input.Layout is not Option<BedLayout>.Some layout || input.YieldAreaM2 is Option<decimal>.None)
                        return Failure("Для цели по м² задайте схему и выделенную площадь урожая");
                    requiredArea = Option.Some(target.Value / rate.Value);
                    perPlant = checked(rate.Value * layout.Value.PlantStep * layout.Value.RowStep);
                }
                plants = checked((int)decimal.Ceiling(target.Value / perPlant));
            }
            if (plants <= 0) return Failure("Расчёт должен давать хотя бы одно растение");
            var forecastPlant = input.PerPlantKg.Map(rate => checked(plants * rate));
            var forecastArea = input.PerM2Kg.Match(rate => input.YieldAreaM2.Map(area => checked(area * rate)), () => Option.None<decimal>());
            var deficit = capacity.Map(bed => Math.Max(0, checked(plants - bed.Plants)));
            var areaDeficit = requiredArea.Match(required => input.YieldAreaM2.Map(area => Math.Max(0, required - area)), () => Option.None<decimal>());
            return Result.Success(new HarvestProjection(plants, capacity, forecastPlant, forecastArea, requiredArea, deficit,
                areaDeficit, input.Mode == HarvestPlanningMode.TargetPerM2));
        }
        catch (Exception error) when (error is OverflowException or DivideByZeroException)
        { return Failure("Количество растений или урожай выходит за допустимый диапазон"); }
    }

    private static Result<HarvestProjection> Failure(string error) => Result.Failure<HarvestProjection>(error);
}
