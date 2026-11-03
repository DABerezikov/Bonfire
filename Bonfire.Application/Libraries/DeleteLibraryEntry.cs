using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Application.Libraries;

public enum LibraryEntryKind { Culture, Sort, Producer }

public abstract record LibraryEntry
{
    private LibraryEntry() { }
    public sealed record Culture(CultureId Id) : LibraryEntry;
    public sealed record Sort(SortId Id) : LibraryEntry;
    public sealed record Producer(ProducerId Id) : LibraryEntry;

    public static Result<LibraryEntry> Create(LibraryEntryKind kind, int id) => kind switch
    {
        LibraryEntryKind.Culture => CultureId.Create(id).Bind(key => Result.Success<LibraryEntry>(new Culture(key))),
        LibraryEntryKind.Sort => SortId.Create(id).Bind(key => Result.Success<LibraryEntry>(new Sort(key))),
        LibraryEntryKind.Producer => ProducerId.Create(id).Bind(key => Result.Success<LibraryEntry>(new Producer(key))),
        _ => Result.Failure<LibraryEntry>("Неизвестный справочник")
    };
}

public interface ILibraryEntryDeletionWriter
{
    Task<Result<bool>> DeleteAsync(LibraryEntry entry, CancellationToken cancellationToken);
}
public interface IDeleteLibraryEntry
{
    Task<Result<bool>> ExecuteAsync(LibraryEntryKind kind, int id, CancellationToken cancellationToken = default);
}
public sealed class DeleteLibraryEntry(ILibraryEntryDeletionWriter writer) : IDeleteLibraryEntry
{
    public Task<Result<bool>> ExecuteAsync(LibraryEntryKind kind, int id, CancellationToken cancellationToken = default)
        => LibraryEntry.Create(kind, id).Match(key => writer.DeleteAsync(key, cancellationToken),
            error => Task.FromResult(Result.Failure<bool>(error)));
}
