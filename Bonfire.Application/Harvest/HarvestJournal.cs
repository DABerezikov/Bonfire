using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;

namespace Bonfire.Application.Harvest;

public sealed record HarvestEntryFromViewModel(
    int Id,
    int Version,
    HarvestEntry Entry,
    Option<int> CurrentBatchId,
    Option<int> CurrentCultureId,
    Option<int> CurrentSortId,
    Option<int> CurrentPerennialId);

public sealed record HarvestWrite(Option<HarvestEntryId> Id, int ExpectedVersion, HarvestEntry Entry);

public interface IHarvestJournalReader
{
    Task<Result<ImmutableArray<HarvestEntryFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
}

public interface IHarvestJournalWriter
{
    Task<Result<HarvestEntryFromViewModel>> SaveAsync(HarvestWrite request, CancellationToken cancellationToken);
    Task<Result<int>> DeleteAsync(HarvestEntryId id, int expectedVersion, CancellationToken cancellationToken);
}

public sealed record HarvestSourceFromViewModel(
    string Label,
    HarvestIdentity Identity,
    Option<decimal> ForecastPerPlantKg,
    Option<decimal> ForecastPerM2Kg);

public interface IHarvestSourceReader
{
    Task<Result<ImmutableArray<HarvestSourceFromViewModel>>> ReadSourcesAsync(int seasonYear,
        CancellationToken cancellationToken);
}

public sealed record SaveHarvestFromViewModel(
    Option<int> Id,
    int ExpectedVersion,
    HarvestIdentity Identity,
    DateOnly Date,
    decimal MassKg,
    Option<string> Note);

public interface IManageHarvestJournal
{
    Task<Result<HarvestEntryFromViewModel>> SaveAsync(SaveHarvestFromViewModel input,
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteAsync(int id, int expectedVersion, CancellationToken cancellationToken = default);
}

public sealed class ManageHarvestJournal(IHarvestJournalReader reader, IHarvestJournalWriter writer, TimeProvider time)
    : IManageHarvestJournal
{
    public async Task<Result<HarvestEntryFromViewModel>> SaveAsync(SaveHarvestFromViewModel input,
        CancellationToken cancellationToken = default)
    {
        var id = input.Id.Match(value => HarvestEntryId.Create(value).Bind(x => Result.Success(Option.Some(x))),
            () => Result.Success(Option.None<HarvestEntryId>()));
        if (id.IsFailure) return id.Bind(_ => Result.Failure<HarvestEntryFromViewModel>("Неверный ID"));
        if (input.Id.Match(_ => input.ExpectedVersion <= 0, () => input.ExpectedVersion != 0))
            return Result.Failure<HarvestEntryFromViewModel>("Неверная версия сбора");
        var identity = input.Identity;
        if (input.Id is Option<int>.Some existing)
        {
            var read = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (read.IsFailure) return read.Bind(_ => Result.Failure<HarvestEntryFromViewModel>("Журнал недоступен"));
            var fresh = read.ValueOrThrow().FirstOrDefault(x => x.Id == existing.Value);
            if (fresh is null || fresh.Version != input.ExpectedVersion)
                return Result.Failure<HarvestEntryFromViewModel>("Сбор изменён или удалён; обновите журнал");
            identity = fresh.Entry.Identity;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo
            .ConvertTime(time.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(identity.TimeZoneId)).DateTime);
        return await HarvestMass.Create(input.MassKg)
            .Bind(mass => HarvestEntry.Create(identity, input.Date, mass, input.Note, today))
            .Match(
                entry => writer.SaveAsync(new HarvestWrite(id.ValueOrThrow(), input.ExpectedVersion, entry),
                    cancellationToken),
                error => Task.FromResult(Result.Failure<HarvestEntryFromViewModel>(error))).ConfigureAwait(false);
    }

    public Task<Result<int>> DeleteAsync(int id, int expectedVersion, CancellationToken cancellationToken = default)
        => HarvestEntryId.Create(id).Match(key => expectedVersion > 0
                ? writer.DeleteAsync(key, expectedVersion, cancellationToken)
                : Task.FromResult(Result.Failure<int>("Неверная версия сбора")),
            error => Task.FromResult(Result.Failure<int>(error)));
}