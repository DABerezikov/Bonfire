using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

/// <summary>Свежий поиск имени с совместимостью для исторических пробелов.</summary>
internal static class DbLibraryNameLookup
{
    public static async Task<Option<T>> ReadAsync<T>(IQueryable<T> rows, string name, CancellationToken cancellationToken)
        where T : NamedEntity
    {
        var exact = await rows.OrderBy(row => row.Id).FirstOrDefaultAsync(row => row.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (exact is not null) return Option.Some(exact);
        var names = await rows.OrderBy(row => row.Id).Select(row => new { row.Id, row.Name })
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var match = names.FirstOrDefault(row => LibraryName.NormalizeWhitespace(row.Name) == name);
        if (match is null) return Option.None<T>();
        var entity = await rows.FirstOrDefaultAsync(row => row.Id == match.Id, cancellationToken).ConfigureAwait(false);
        return entity is null ? Option.None<T>() : Option.Some(entity);
    }
}
