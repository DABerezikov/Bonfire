using Bonfire.Domain.Functional;

namespace Bonfire.Application.Caching;

[Flags]
public enum ReadArea
{
    Seeds = 1, Catalog = 2, Seedlings = 4, Garden = 8,
    PlantingPlans = 16, PlanningLibraries = 32, Harvest = 64, Perennials = 128, SortOptions = 256
}

public interface IReadModelCache
{
    long Version(ReadArea areas);
    void Invalidate(ReadArea areas);
    Task<Result<T>> ReadAsync<T>(ReadArea areas, object key, Func<CancellationToken, Task<Result<T>>> read,
        CancellationToken cancellationToken = default);
}

/// <summary>Кэш независимых снимков. Отказы не сохраняются, запись меняет версии затронутых областей.</summary>
public sealed class ReadModelCache : IReadModelCache
{
    private readonly Lock _gate = new();
    private readonly Dictionary<ReadArea, long> _versions = new();
    private readonly Dictionary<(Type Type, ReadArea Areas, object Key), object> _entries = new();
    private long _sequence;

    public long Version(ReadArea areas)
    {
        lock (_gate) return CurrentVersion(areas);
    }

    private long CurrentVersion(ReadArea areas) => _versions.Where(pair => (pair.Key & areas) != 0)
        .Select(pair => pair.Value).DefaultIfEmpty(0).Max();

    public void Invalidate(ReadArea areas)
    {
        lock (_gate)
        {
            var version = ++_sequence;
            foreach (var area in Enum.GetValues<ReadArea>())
                if ((area & areas) != 0) _versions[area] = version;
            foreach (var key in _entries.Keys.Where(key => (key.Areas & areas) != 0).ToArray()) _entries.Remove(key);
        }
    }

    public async Task<Result<T>> ReadAsync<T>(ReadArea areas, object key,
        Func<CancellationToken, Task<Result<T>>> read, CancellationToken cancellationToken = default)
    {
        var identity = (typeof(T), areas, key);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cached<T> entry;
            lock (_gate)
            {
                var version = CurrentVersion(areas);
                if (_entries.TryGetValue(identity, out var existing) && existing is Cached<T> cached && cached.Version == version)
                    entry = cached;
                else
                {
                    entry = new Cached<T>(version, new Lazy<Task<Result<T>>>(async () =>
                        await read(CancellationToken.None).ConfigureAwait(false), LazyThreadSafetyMode.ExecutionAndPublication));
                    _entries[identity] = entry;
                }
            }
            var operation = entry.Operation.Value;
            try
            {
                var result = await operation.WaitAsync(cancellationToken).ConfigureAwait(false);
                lock (_gate)
                {
                    if (entry.Version != CurrentVersion(areas)) continue;
                }
                return result;
            }
            finally
            {
                if (operation.IsFaulted || operation.IsCanceled || operation.IsCompletedSuccessfully && operation.Result.IsFailure)
                    lock (_gate)
                        if (_entries.TryGetValue(identity, out var current) && ReferenceEquals(current, entry)) _entries.Remove(identity);
            }
        }
    }

    private sealed record Cached<T>(long Version, Lazy<Task<Result<T>>> Operation);
}
