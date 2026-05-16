using Bonfire.Models;
using System.Collections.Generic;
using System.Linq;

namespace Bonfire.Services.Extensions;

public static class ViewModelsExtensions
{
    public static IOrderedEnumerable<SeedlingFromViewModel> SortSeedlings(
        this IEnumerable<SeedlingFromViewModel> collection)
    {
        return collection.OrderBy(c => c.LandingData)
            .ThenBy(c => c.Culture)
            .ThenBy(s => s.Sort)
            .ThenBy(p => p.Producer);
    }

    public static IOrderedEnumerable<SeedsFromViewModel> SortSeeds(this IEnumerable<SeedsFromViewModel> collection)
    {
        return collection.OrderBy(c => c.Culture)
            .ThenBy(s => s.Sort)
            .ThenBy(p => p.Producer);
    }
}