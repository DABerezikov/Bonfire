using System.Diagnostics;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Seeds;

namespace Bonfire.Domain.Garden;

public abstract record PlantingSource
{
    private PlantingSource()
    {
    }

    public sealed record Seedlings : PlantingSource
    {
        public Seedlings(SeedlingId id)
        {
            ArgumentNullException.ThrowIfNull(id);
            Id = id;
        }

        public SeedlingId Id { get; }
    }

    public sealed record Seeds : PlantingSource
    {
        public Seeds(SeedId id, SeedUnit unit)
        {
            ArgumentNullException.ThrowIfNull(id);
            (Id, Unit) = (id, unit);
        }

        public SeedId Id { get; }
        public SeedUnit Unit { get; }
    }
}

public sealed class SpotPlanting
{
    private SpotPlanting(int spotId, PlantingSource source, SeedAmount amount, DateTime date, string label,
        Option<string> place)
        => (SpotId, Source, Amount, Date, Label, Place) = (spotId, source, amount, date, label, place);

    public int SpotId { get; }
    public PlantingSource Source { get; }
    public SeedAmount Amount { get; }
    public DateTime Date { get; }
    public string Label { get; }
    public Option<string> Place { get; }

    public static Result<SpotPlanting> Create(int spotId, PlantingSource source, double amount, DateTime date,
        string label, Option<string> place)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(place);
        var unit = source switch
        {
            PlantingSource.Seedlings => SeedUnit.Pieces,
            PlantingSource.Seeds seeds => seeds.Unit,
            _ => throw new UnreachableException()
        };
        return spotId <= 0 || date == default
            ? Result.Failure<SpotPlanting>("Укажите ячейку и дату посадки")
            : SeedAmount.Create(amount, unit).Bind(value => value.Value <= 0
                ? Result.Failure<SpotPlanting>("Количество для посадки должно быть положительным")
                : Result.Success(new SpotPlanting(spotId, source, value, date, label, place)));
    }
}