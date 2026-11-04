using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public sealed record SeedQuantityCalculation(
    PlantCount Seeds,
    Option<decimal> ExpectedPlants,
    bool IsManual,
    bool HasDeficit)
{
    public static Result<SeedQuantityCalculation> Automatic(PlantCount target, GerminationPercentage germination,
        LossPercentage losses)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(germination);
        ArgumentNullException.ThrowIfNull(losses);
        var survival = germination.Value / 100m * (1m - losses.Value / 100m);
        if (survival <= 0)
            return Result.Failure<SeedQuantityCalculation>("Проценты слишком малы для точного расчёта количества");
        return DecimalArithmetic.Divide(target.Value, survival)
            .Bind(seeds => DecimalArithmetic.ToInt32(decimal.Ceiling(seeds)))
            .Bind(PlantCount.Create).Bind(count =>
                Result.Success(new SeedQuantityCalculation(count, Option.Some(count.Value * survival), false, false)))
            .Match(Result.Success,
                _ => Result.Failure<SeedQuantityCalculation>("Число семян выходит за допустимый диапазон"));
    }

    public static SeedQuantityCalculation Manual(PlantCount target, PlantCount seeds,
        Option<GerminationPercentage> germination, Option<LossPercentage> losses)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(seeds);
        var expected = germination.Match(g => losses.Map(l => seeds.Value * (g.Value / 100m) * (1m - l.Value / 100m)),
            () => Option.None<decimal>());
        return new SeedQuantityCalculation(seeds, expected, true,
            expected.Match(value => value < target.Value, () => false));
    }
}