using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Seeds;

public sealed record SortCharacteristicsValues
{
    public Option<string> Description { get; init; } = Option.None<string>();
    public Option<int> MinGerminationTime { get; init; } = Option.None<int>();
    public Option<int> MaxGerminationTime { get; init; } = Option.None<int>();
    public Option<int> AgeOfSeedlings { get; init; } = Option.None<int>();
    public Option<int> MaxAgeOfSeedlings { get; init; } = Option.None<int>();
    public Option<int> GrowingSeason { get; init; } = Option.None<int>();
    public Option<int> MaxGrowingSeason { get; init; } = Option.None<int>();
    public Option<int> LandingPattern { get; init; } = Option.None<int>();
    public Option<int> LandingRowSpacing { get; init; } = Option.None<int>();
    public Option<int> PlantHeight { get; init; } = Option.None<int>();
    public Option<int> MaxPlantHeight { get; init; } = Option.None<int>();
    public Option<string> PlantColor { get; init; } = Option.None<string>();
    public Option<string> Maturity { get; init; } = Option.None<string>();
    public Option<double> YieldKg { get; init; } = Option.None<double>();
    public Option<int> MinFruitSize { get; init; } = Option.None<int>();
    public Option<int> MaxFruitSize { get; init; } = Option.None<int>();
    public Option<double> MinFruitWeight { get; init; } = Option.None<double>();
    public Option<double> MaxFruitWeight { get; init; } = Option.None<double>();
}

public sealed class SortCharacteristics
{
    private SortCharacteristics(LibraryName name, SortCharacteristicsValues values) => (Name, Values) = (name, values);
    public LibraryName Name { get; }
    public SortCharacteristicsValues Values { get; }

    public static Result<SortCharacteristics> Create(string name, SortCharacteristicsValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return LibraryName.Create(name).Bind(validName => IsValid(values)
            ? Result.Success(new SortCharacteristics(validName, values))
            : Result.Failure<SortCharacteristics>("Исправьте отрицательные значения и диапазоны характеристик сорта"));
    }

    private static bool IsValid(SortCharacteristicsValues values) =>
        ValidRange(values.MinGerminationTime, values.MaxGerminationTime)
        && ValidRange(values.AgeOfSeedlings, values.MaxAgeOfSeedlings)
        && ValidRange(values.GrowingSeason, values.MaxGrowingSeason)
        && ValidRange(values.PlantHeight, values.MaxPlantHeight)
        && ValidRange(values.MinFruitSize, values.MaxFruitSize)
        && ValidRange(values.MinFruitWeight, values.MaxFruitWeight)
        && ValidRange(values.LandingPattern, Option.None<int>())
        && ValidRange(values.LandingRowSpacing, Option.None<int>())
        && ValidRange(values.YieldKg, Option.None<double>());

    private static bool ValidRange(Option<int> min, Option<int> max)
        => ValidRange(min.Map(value => (double)value), max.Map(value => (double)value));

    private static bool ValidRange(Option<double> min, Option<double> max)
        => min.Match(value => double.IsFinite(value) && value >= 0
                                                     && max.Match(upper => value <= upper, () => true), () => true)
           && max.Match(value => double.IsFinite(value) && value >= 0, () => true);

    public Result<SortCharacteristics> WithDetails(string name, SortCharacteristicsValues values) =>
        Create(name, values);
}