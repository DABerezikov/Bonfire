using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seedlings;

public sealed record SeedlingEntry(Option<int> Id, int Number, Option<DateTime> GerminationDate, Option<bool> IsDead);

public sealed class SeedlingBatch
{
    public double Quantity { get; }
    public ImmutableArray<SeedlingEntry> Entries { get; }
    public int GerminatedCount => Entries.Count(entry => entry.GerminationDate is Option<DateTime>.Some);
    public int AliveCount => Entries.Count(entry => entry.GerminationDate is Option<DateTime>.Some && entry.IsDead is not Option<bool>.Some { Value: true });

    private SeedlingBatch(double quantity, ImmutableArray<SeedlingEntry> entries) => (Quantity, Entries) = (quantity, entries);

    public static Result<SeedlingBatch> Create(double quantity, IEnumerable<SeedlingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = entries.ToImmutableArray();
        return !double.IsFinite(quantity) || quantity < 0
            ? Result.Failure<SeedlingBatch>("Количество рассады должно быть конечным и неотрицательным")
            : snapshot.Any(entry => entry.Id.Match(id => id <= 0, () => false))
                ? Result.Failure<SeedlingBatch>("Некорректный идентификатор записи рассады")
                : Result.Success(new SeedlingBatch(quantity, snapshot));
    }

    public Result<SeedlingBatch> WithGermination(int count, DateTime date)
    {
        if (count < 0) return Result.Failure<SeedlingBatch>("Число всходов не может быть отрицательным");
        if (count == 0)
            return Entries.Length == 0
                ? Result.Failure<SeedlingBatch>("В партии нет записи для отметки отсутствия всходов")
                : Result.Success(new SeedlingBatch(Quantity, Entries.Length == 1
                    ? [Entries[0] with { IsDead = Option.Some(true) }]
                    : Entries));

        var added = Quantity == 0 ? count : (int)Math.Min(count, Math.Max(0, Math.Floor(Quantity) - GerminatedCount));
        if (added > int.MaxValue - GerminatedCount)
            return Result.Failure<SeedlingBatch>("Число всходов превышает допустимый диапазон");
        var newEntries = Enumerable.Range(0, added).Select(index => new SeedlingEntry(
            Option.None<int>(), GerminatedCount + index + 1, Option.Some(date), Option.None<bool>()));
        return Result.Success(new SeedlingBatch(Quantity, Entries.AddRange(newEntries)));
    }

    public Result<SeedlingBatch> Apply(SeedlingAction action) => action switch
    {
        SeedlingAction.Germinate germinate => WithGermination(germinate.Count, germinate.Date),
        _ => throw new System.Diagnostics.UnreachableException()
    };
}

public abstract record SeedlingAction
{
    private SeedlingAction() { }
    public sealed record Germinate(int Count, DateTime Date) : SeedlingAction;
}
