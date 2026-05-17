using ObservableCollectionExtensions = Bonfire.Services.Extensions.ObservableCollectionExtensions;

namespace Services.Tests;

public class ObservableCollectionExtensionsTests
{
    // ── Add(IEnumerable) ──────────────────────────────────────────────────────

    [Fact]
    public void Add_AddsAllItemsToCollection()
    {
        var collection = new ObservableCollection<int> { 1 };
        ObservableCollectionExtensions.AddRange(collection, new[] { 2, 3, 4 });
        Assert.Equal(new[] { 1, 2, 3, 4 }, collection);
    }

    [Fact]
    public void Add_EmptySource_CollectionUnchanged()
    {
        var collection = new ObservableCollection<int> { 1, 2 };
        ObservableCollectionExtensions.AddRange(collection, Array.Empty<int>());
        Assert.Equal(new[] { 1, 2 }, collection);
    }

    [Fact]
    public void Add_ToEmptyCollection_AddsAllItems()
    {
        var collection = new ObservableCollection<string>();
        ObservableCollectionExtensions.AddRange(collection, new[] { "a", "b", "c" });
        Assert.Equal(new[] { "a", "b", "c" }, collection);
    }

    // ── AddClear ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddClear_ClearsExistingThenAddsNew()
    {
        var collection = new ObservableCollection<int> { 10, 20 };
        collection.AddClear(new[] { 1, 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, collection);
    }

    [Fact]
    public void AddClear_EmptySource_CollectionBecomesEmpty()
    {
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        collection.AddClear(Array.Empty<int>());
        Assert.Empty(collection);
    }

    [Fact]
    public void AddClear_OnEmptyCollection_AddsItems()
    {
        var collection = new ObservableCollection<string>();
        collection.AddClear(new[] { "x", "y" });
        Assert.Equal(new[] { "x", "y" }, collection);
    }
}
