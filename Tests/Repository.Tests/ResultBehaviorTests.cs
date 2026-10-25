using Bonfire.Domain.Functional;

namespace Repository.Tests;

public class ResultBehaviorTests
{
    [Fact]
    public void Success_PreservesValueAndSelectsSuccessBranch()
    {
        var value = new object();
        var result = Result.Success(value);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.True(result.Match(_ => true, _ => false));
        Assert.Same(value, result.ValueOrThrow());
        Assert.Same(value, result.ValueOrDefault(new object()));
        Assert.Equal("ok", result.Match(_ => "ok", _ => throw new Exception("Wrong branch")));
    }

    [Fact]
    public void Failure_PreservesErrorAndDoesNotInvokeBinder()
    {
        var result = Result.Failure<int>("Ошибка сохранения");
        Assert.True(result.IsFailure);
        Assert.Equal(42, result.ValueOrDefault(42));
        Assert.Equal("Ошибка сохранения", Assert.Throws<InvalidOperationException>(() => result.ValueOrThrow()).Message);
        var bound = result.Bind<string>(_ => throw new Exception("Must not run"));
        Assert.True(bound.IsFailure);
        Assert.Equal(result.Match(_ => string.Empty, error => error), bound.Match(_ => string.Empty, error => error));
        Assert.Equal(result.Match(_ => string.Empty, error => error), result.Match(_ => throw new Exception("Wrong branch"), error => error));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Failure_EmptyMessage_IsRejected(string? error)
        => Assert.Throws<ArgumentException>(() => Result.Failure<int>(error!));

    [Fact]
    public void Bind_Success_CanTransformOrReturnFailure()
    {
        Assert.Equal("7", Result.Success(7).Bind(n => Result.Success(n.ToString())).ValueOrThrow());
        Assert.Equal("нет", Result.Success(7).Bind(_ => Result.Failure<int>("нет")).Match(_ => string.Empty, error => error));
    }

    [Fact]
    public void FunctionalOperations_NullDelegates_AreRejected()
    {
        var result = Result.Success(1);
        Assert.Throws<ArgumentNullException>(() => result.Bind<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(_ => 0, null!));
    }

    [Fact]
    public void From_MissingValue_ReturnsFailureInsteadOfNullableSuccess()
    {
        Assert.True(Result.From<string>(null!).IsFailure);
        Assert.Equal("ошибка", Result.From(10, "ошибка").Match(_ => string.Empty, error => error));
    }

    [Fact]
    public void Success_NullValue_IsRejected()
        => Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
}
