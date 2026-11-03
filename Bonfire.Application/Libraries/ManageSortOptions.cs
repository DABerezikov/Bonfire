using System.Collections.Immutable;
using Bonfire.Application.Caching;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Application.Libraries;

public sealed record SortOptionFromViewModel(SortOptionCategory Category, string Name, bool IsUsed);

public interface ISortOptionStore
{
    Task<Result<ImmutableArray<SortOptionFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
    Task<Result<SortOptionFromViewModel>> SaveAsync(Option<SortOptionName> original, SortOptionName value, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(SortOptionName value, CancellationToken cancellationToken);
}

public interface IManageSortOptions
{
    long ReadVersion { get; }
    void RefreshReadCache();
    Task<Result<ImmutableArray<SortOptionFromViewModel>>> ReadAsync(CancellationToken cancellationToken = default);
    Task<Result<SortOptionFromViewModel>> SaveAsync(SortOptionCategory category, Option<string> original, string name, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(SortOptionCategory category, string name, CancellationToken cancellationToken = default);
}

public sealed class ManageSortOptions(ISortOptionStore store, IReadModelCache cache) : IManageSortOptions
{
    public long ReadVersion => cache.Version(ReadArea.Catalog | ReadArea.SortOptions);
    public void RefreshReadCache() => cache.Invalidate(ReadArea.Catalog | ReadArea.SortOptions);
    public Task<Result<ImmutableArray<SortOptionFromViewModel>>> ReadAsync(CancellationToken cancellationToken = default)
        => cache.ReadAsync(ReadArea.Catalog | ReadArea.SortOptions, nameof(ManageSortOptions), store.ReadAsync, cancellationToken);

    public Task<Result<SortOptionFromViewModel>> SaveAsync(SortOptionCategory category, Option<string> original, string name, CancellationToken cancellationToken = default)
        => SortOptionName.Create(category, name).Bind(value => original.Match(
            text => SortOptionName.Create(category, text).Bind(old => Result.Success((Original: Option.Some(old), Value: value))),
            () => Result.Success((Original: Option.None<SortOptionName>(), Value: value))))
            .Match(change => store.SaveAsync(change.Original, change.Value, cancellationToken),
                error => Task.FromResult(Result.Failure<SortOptionFromViewModel>(error)));

    public Task<Result<bool>> DeleteAsync(SortOptionCategory category, string name, CancellationToken cancellationToken = default)
        => SortOptionName.Create(category, name).Match(value => store.DeleteAsync(value, cancellationToken),
            error => Task.FromResult(Result.Failure<bool>(error)));
}
