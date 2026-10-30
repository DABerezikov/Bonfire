using Bonfire.Domain.Functional;

namespace Models.Tests;

public class OptionTests
{
    [Fact]
    public void Map_PresentValue_TransformsIt()
    {
        var result = Option.Some(3).Map(value => value.ToString());
        Assert.Equal("3", result.Match(value => value, () => "нет"));
    }

    [Fact]
    public void Map_AbsentValue_DoesNotInvokeMapper()
    {
        var result = Option.None<int>().Map<int, string>(_ => throw new Exception("Не вызывать"));
        Assert.Equal("нет", result.Match(value => value, () => "нет"));
    }

    [Fact]
    public void Map_Identity_PreservesPresentAndAbsentValues()
    {
        var some = Option.Some(5);
        var none = Option.None<int>();
        Assert.Equal(some, some.Map(value => value));
        Assert.Equal(none, none.Map(value => value));
    }

    [Fact]
    public void Some_NullValue_IsRejected()
        => Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));

    [Fact]
    public void Match_AbsentValue_DoesNotInvokePresentBranch()
        => Assert.Equal(4, Option.None<string>().Match<int>(_ => throw new Exception("Не вызывать"), () => 4));
}
