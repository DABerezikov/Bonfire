using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seedlings;

public sealed record ReplantingEntry(Option<int> Id, DateTime Date, Option<double> PotVolume, Option<string> Note);
public sealed record SeedlingEntry(Option<int> Id, int Number, Option<DateTime> GerminationDate, Option<bool> IsDead)
{
    public Option<string> DeathNote { get; init; } = Option.None<string>();
    public ImmutableArray<ReplantingEntry> Replants { get; init; } = [];
}

public sealed class SeedlingBatch
{
    public double Quantity { get; }
    public int PlantedOut { get; }
    public int AvailableCount => AliveCount - PlantedOut;
    public ImmutableArray<SeedlingEntry> Entries { get; }
    public int GerminatedCount => Entries.Count(entry => entry.GerminationDate is Option<DateTime>.Some);
    public int AliveCount => Entries.Count(entry => entry.GerminationDate is Option<DateTime>.Some && entry.IsDead is not Option<bool>.Some { Value: true });

    private SeedlingBatch(double quantity, ImmutableArray<SeedlingEntry> entries, int plantedOut)
        => (Quantity, Entries, PlantedOut) = (quantity, entries, plantedOut);

    public static Result<SeedlingBatch> Create(double quantity, IEnumerable<SeedlingEntry> entries, int plantedOut = 0)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = entries.ToImmutableArray();
        return !double.IsFinite(quantity) || quantity < 0
            ? Result.Failure<SeedlingBatch>("Количество рассады должно быть конечным и неотрицательным")
            : snapshot.Any(entry => entry.Id.Match(id => id <= 0, () => false))
                ? Result.Failure<SeedlingBatch>("Некорректный идентификатор записи рассады")
                : plantedOut < 0
                    ? Result.Failure<SeedlingBatch>("Количество высаженной рассады не может быть отрицательным")
                    : Result.Success(new SeedlingBatch(quantity, snapshot, plantedOut));
    }

    public Result<SeedlingBatch> WithGermination(int count, DateTime date)
    {
        if (count < 0) return Result.Failure<SeedlingBatch>("Число всходов не может быть отрицательным");
        if (count == 0)
            return Entries.Length == 0
                ? Result.Failure<SeedlingBatch>("В партии нет записи для отметки отсутствия всходов")
                : Result.Success(new SeedlingBatch(Quantity, Entries.Length == 1
                    ? [Entries[0] with { IsDead = Option.Some(true) }]
                    : Entries, PlantedOut));

        var germinated = GerminatedCount;
        var added = Quantity == 0 ? count : (int)Math.Min(count, Math.Max(0, Math.Floor(Quantity) - germinated));
        if (added > int.MaxValue - germinated)
            return Result.Failure<SeedlingBatch>("Число всходов превышает допустимый диапазон");
        var newEntries = Enumerable.Range(0, added).Select(index => new SeedlingEntry(
            Option.None<int>(), germinated + index + 1, Option.Some(date), Option.None<bool>()));
        return Result.Success(new SeedlingBatch(Quantity, Entries.AddRange(newEntries), PlantedOut));
    }

    public Result<SeedlingBatch> Apply(SeedlingAction action) => action switch
    {
        SeedlingAction.Germinate germinate => WithGermination(germinate.Count, germinate.Date),
        SeedlingAction.Replant replant => WithReplanting(replant.Date),
        SeedlingAction.MarkDead death => WithDeadSprouts(death.Count, death.Reason),
        _ => throw new System.Diagnostics.UnreachableException()
    };

    public Result<SeedlingBatch> WithReplanting(DateTime date) => Entries.Length == 0
        ? Result.Failure<SeedlingBatch>("В партии нет записи для пересадки")
        : !Entries[0].Replants.IsEmpty
            ? Result.Failure<SeedlingBatch>("Пересадка партии уже записана")
            : Result.Success(new SeedlingBatch(Quantity, Entries.SetItem(0, Entries[0] with
            {
                Replants = [new(Option.None<int>(), date, Option.None<double>(), Option.None<string>())]
            }), PlantedOut));

    public Result<SeedlingBatch> WithDeadSprouts(int count, Option<string> reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        if (count < 0) return Result.Failure<SeedlingBatch>("Число погибших всходов не может быть отрицательным");
        var selected = Entries.Where(entry => entry.GerminationDate is Option<DateTime>.Some && entry.IsDead is not Option<bool>.Some { Value: true })
            .OrderByDescending(entry => entry.Number).Take(count).ToHashSet();
        return Result.Success(new SeedlingBatch(Quantity, Entries.Select(entry => selected.Contains(entry)
            ? entry with { IsDead = Option.Some(true), DeathNote = reason }
            : entry).ToImmutableArray(), PlantedOut));
    }
}

public abstract record SeedlingAction
{
    private SeedlingAction() { }
    public sealed record Germinate(int Count, DateTime Date) : SeedlingAction;
    public sealed record Replant(DateTime Date) : SeedlingAction;
    public sealed record MarkDead(int Count, Option<string> Reason) : SeedlingAction;
}
