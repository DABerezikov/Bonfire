namespace Services.Tests;

public class ObservableCollectionExtensionsTests
{
    // ── AddRange (встроенный .NET 10) ─────────────────────────────────────────

    [Fact]
    public void AddRange_AddsAllItemsToCollection()
    {
        var collection = new ObservableCollection<int> { 1 };
        collection.AddRange([2, 3, 4]);
        Assert.Equal(new[] { 1, 2, 3, 4 }, collection);
    }

    [Fact]
    public void AddRange_EmptySource_CollectionUnchanged()
    {
        var collection = new ObservableCollection<int> { 1, 2 };
        collection.AddRange([]);
        Assert.Equal(new[] { 1, 2 }, collection);
    }

    [Fact]
    public void AddRange_ToEmptyCollection_AddsAllItems()
    {
        var collection = new ObservableCollection<string>();
        collection.AddRange(["a", "b", "c"]);
        Assert.Equal(new[] { "a", "b", "c" }, collection);
    }

    // ── AddClear ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddClear_ClearsExistingThenAddsNew()
    {
        var collection = new ObservableCollection<int> { 10, 20 };
        collection.AddClear([1, 2, 3]);
        Assert.Equal(new[] { 1, 2, 3 }, collection);
    }

    [Fact]
    public void AddClear_EmptySource_CollectionBecomesEmpty()
    {
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        collection.AddClear([]);
        Assert.Empty(collection);
    }

    [Fact]
    public void AddClear_OnEmptyCollection_AddsItems()
    {
        var collection = new ObservableCollection<string>();
        collection.AddClear(["x", "y"]);
        Assert.Equal(new[] { "x", "y" }, collection);
    }
}
