using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Domain.Seedlings;

public sealed record SeedlingDetailsValues
{
    public double Quantity { get; init; }
    public double Weight { get; init; }
    public Option<DateTime> LandingDate { get; init; } = Option.None<DateTime>();
    public Option<string> Source { get; init; } = Option.None<string>();
    public Option<string> Place { get; init; } = Option.None<string>();
    public Option<string> Note { get; init; } = Option.None<string>();
    public string LunarPhase { get; init; } = string.Empty;
}

public sealed class SeedlingDetails
{
    public SeedlingDetailsValues Values { get; }
    public DateTime LandingDate { get; }
    public string Source { get; }
    private SeedlingDetails(SeedlingDetailsValues values, DateTime date, string source)
        => (Values, LandingDate, Source) = (values, date, source);

    public static Result<SeedlingDetails> Create(SeedlingDetailsValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return SeedAmount.Create(values.Quantity, SeedUnit.Pieces).Bind(_ =>
            SeedAmount.Create(values.Weight, SeedUnit.Grams).Bind(_ =>
                values.LandingDate.Match(date => values.Source.Match(
                    source => Result.Success(new SeedlingDetails(values, date, source)),
                    () => Result.Failure<SeedlingDetails>("Укажите источник рассады")),
                    () => Result.Failure<SeedlingDetails>("Укажите дату посева"))));
    }
}

public sealed record SeedlingEntryEdit(int Id, int Number, Option<DateTime> GerminationDate, Option<string> DeathNote);

public sealed class SeedlingEdit
{
    public SeedlingId Id { get; }
    public SeedlingDetails Details { get; }
    public ImmutableArray<SeedlingEntryEdit> Entries { get; }
    private SeedlingEdit(SeedlingId id, SeedlingDetails details, ImmutableArray<SeedlingEntryEdit> entries)
        => (Id, Details, Entries) = (id, details, entries);

    public static Result<SeedlingEdit> Create(int id, SeedlingDetails details, IEnumerable<SeedlingEntryEdit> entries)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(entries);
        var snapshot = entries.ToImmutableArray();
        return snapshot.Any(entry => entry.Id <= 0) || snapshot.Select(entry => entry.Id).Distinct().Count() != snapshot.Length
            ? Result.Failure<SeedlingEdit>("Некорректные идентификаторы записей рассады")
            : SeedlingId.Create(id).Bind(value => Result.Success(new SeedlingEdit(value, details, snapshot)));
    }

    public Result<SeedlingBatch> ApplyTo(SeedlingBatch current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (Entries.Any(edit => !current.Entries.Any(entry => entry.Id.Match(id => id == edit.Id, () => false))))
            return Result.Failure<SeedlingBatch>("Запись рассады удалена или относится к другой партии");
        var changes = Entries.ToDictionary(entry => entry.Id);
        return SeedlingBatch.Create(Details.Values.Quantity, current.Entries.Select(entry => entry.Id.Match(
            id => changes.TryGetValue(id, out var edit)
                ? entry with { Number = edit.Number, GerminationDate = edit.GerminationDate, DeathNote = edit.DeathNote }
                : entry,
            () => entry)));
    }
}
