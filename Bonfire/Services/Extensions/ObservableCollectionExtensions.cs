using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonfire.Services.Extensions;

public static class ObservableCollectionExtensions
{
    public static void AddClear<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        collection.AddRange(items);
    }
}
