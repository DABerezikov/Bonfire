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
        var along = layout.RowsAcross ? layout.Width : layout.Length;
        var across = layout.RowsAcross ? layout.Length : layout.Width;
        return Count(along, layout.StartMargin, layout.EndMargin, layout.PlantStep).Bind(perRow =>
            Count(across, layout.LeftMargin, layout.RightMargin, layout.RowStep).Bind(rows =>
            rows != 0 && perRow > int.MaxValue / rows
                ? Result.Failure<BedCapacity>("Число мест выходит за допустимый диапазон")
                : DecimalArithmetic.Multiply(layout.Length, layout.Width).Bind(area =>
                    DecimalArithmetic.Multiply(layout.PlantStep, layout.RowStep).Bind(feedingArea => DecimalArithmetic.Divide(1m, feedingArea)
                    .Bind(density => Result.Success(new BedCapacity(rows, perRow, rows * perRow, area, density)))))));
    }

    private static Result<int> Count(decimal size, decimal before, decimal after, decimal step)
    {
        if (before > size || after > size - before) return Result.Success(0);
        return DecimalArithmetic.Divide(size - before - after, step).Bind(count =>
            count >= int.MaxValue ? Result.Failure<int>("Число позиций выходит за допустимый диапазон")
                : Result.Success((int)decimal.Floor(count) + 1));
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
        return ResolveTarget(input, manualPlants, capacity).Bind(target =>
        {
            if (target.Plants <= 0) return Failure("Расчёт должен давать хотя бы одно растение");
            var areaForecast = input.PerM2Kg.Match(rate => input.YieldAreaM2.Match(area => DecimalArithmetic.Multiply(area, rate)
                .Bind(value => Result.Success(Option.Some(value))), () => Result.Success(Option.None<decimal>())), () => Result.Success(Option.None<decimal>()));
            return input.PerPlantKg.Match(rate => DecimalArithmetic.Multiply(target.Plants, rate).Bind(value => Result.Success(Option.Some(value))),
                () => Result.Success(Option.None<decimal>())).Bind(plantForecast => areaForecast.Bind(area =>
                Result.Success(new HarvestProjection(target.Plants, capacity, plantForecast, area, target.Area,
                    capacity.Map(bed => Math.Max(0, target.Plants - bed.Plants)),
                    target.Area.Match(required => input.YieldAreaM2.Map(allocated => Math.Max(0, required - allocated)), () => Option.None<decimal>()),
                    input.Mode == HarvestPlanningMode.TargetPerM2))));
        });
    }

    private static Result<(int Plants, Option<decimal> Area)> ResolveTarget(HarvestPlanningInput input, int manualPlants, Option<BedCapacity> capacity)
    {
        if (input.Mode == HarvestPlanningMode.ManualPlants) return Result.Success((manualPlants, Option.None<decimal>()));
        if (input.Mode == HarvestPlanningMode.BedCapacity) return capacity.Match(bed => Result.Success((bed.Plants, Option.None<decimal>())),
            () => Result.Failure<(int, Option<decimal>)>("Задайте размеры и схему грядки"));
        if (input.TargetKg is not Option<decimal>.Some target) return Result.Failure<(int, Option<decimal>)>("Задайте желаемый урожай в кг");
        var selected = input.Mode == HarvestPlanningMode.TargetPerPlant ? input.PerPlantKg : input.PerM2Kg;
        if (selected is not Option<decimal>.Some rate || rate.Value <= 0) return Result.Failure<(int, Option<decimal>)>("Для цели нужна положительная выбранная норма");
        if (input.Mode == HarvestPlanningMode.TargetPerPlant)
            return RequiredPlants(target.Value, rate.Value).Bind(plants => Result.Success((plants, Option.None<decimal>())));
        if (input.Layout is not Option<BedLayout>.Some layout || input.YieldAreaM2 is Option<decimal>.None)
            return Result.Failure<(int, Option<decimal>)>("Для цели по м² задайте схему и выделенную площадь урожая");
        var equivalentRate = DecimalArithmetic.Multiply(rate.Value, layout.Value.PlantStep)
            .Bind(plantRate => DecimalArithmetic.Multiply(plantRate, layout.Value.RowStep));
        return DecimalArithmetic.Divide(target.Value, rate.Value).Bind(area => equivalentRate.Bind(equivalent =>
            RequiredPlants(target.Value, equivalent).Bind(plants => Result.Success((plants, Option.Some(area))))));
    }

    private static Result<int> RequiredPlants(decimal target, decimal rate) => DecimalArithmetic.Divide(target, rate)
        .Bind(value => DecimalArithmetic.ToInt32(decimal.Ceiling(value)));

    private static Result<HarvestProjection> Failure(string error) => Result.Failure<HarvestProjection>(error);
}
