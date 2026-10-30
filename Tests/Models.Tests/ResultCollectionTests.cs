using Bonfire.Domain.Functional;

namespace Models.Tests;

public class ResultCollectionTests
{
    [Fact]
    public void Traverse_Success_PreservesOrderAndEmptyCollection()
    {
        Assert.Equal(new[] { 6, 2, 4 }, new[] { 3, 1, 2 }.Traverse(value => Result.Success(value * 2)).ValueOrThrow());
        Assert.Empty(Array.Empty<int>().Traverse(Result.Success).ValueOrThrow());
    }

    [Fact]
    public void Traverse_FirstFailure_DoesNotMapFollowingItems()
    {
        var seen = new List<int>();
        var result = new[] { 1, 2, 3 }.Traverse(value =>
        {
            seen.Add(value);
            return value == 2 ? Result.Failure<int>("Первый отказ") : Result.Success(value);
        });
        Assert.Equal("Первый отказ", result.Match(_ => "", error => error));
        Assert.Equal(new[] { 1, 2 }, seen);
    }
}
