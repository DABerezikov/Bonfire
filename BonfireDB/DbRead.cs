using System.Data.Common;
using Bonfire.Domain.Functional;

namespace BonfireDB;

internal static class DbRead
{
    public static async Task<Result<T>> TryAsync<T>(Func<Task<Result<T>>> read, string subject)
    {
        ArgumentNullException.ThrowIfNull(read);
        try { return await read().ConfigureAwait(false); }
        catch (DbException error) { return Result.Failure<T>($"Ошибка чтения {subject}: {error.Message}"); }
    }
}
