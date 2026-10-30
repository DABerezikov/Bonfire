using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Services.Tests;

public class SowingApplicationTests
{
    private readonly ISowingSessionFactory _sessions = Substitute.For<ISowingSessionFactory>();
    private readonly ISowingSession _session = Substitute.For<ISowingSession>();
    private readonly ILunarCalendar _calendar = Substitute.For<ILunarCalendar>();

    public SowingApplicationTests()
    {
        _sessions.Create().Returns(_session);
        _calendar.GetPhase(Arg.Any<DateTime>()).Returns("Фаза");
        _session.LoadStockAsync(1, SeedUnit.Pieces, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new SeedStock(SeedAmount.Create(10, SeedUnit.Pieces).ValueOrThrow())));
        _session.SaveAsync(Arg.Any<SowingChange>(), Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>())
            .Returns(new SavedSowing(42, 8, Option.None<double>()));
    }

    private static SowingRequest Request => new(1, 2, SeedUnit.Pieces, new DateTime(2026, 3, 1), "Посеяно", Option.Some("Окно"));

    [Fact]
    public async Task Execute_ValidRequest_ReturnsSavedReceiptAndReleasesSession()
    {
        var result = await new SowSeeds(_sessions, _calendar).ExecuteAsync(Request);
        var receipt = result.ValueOrThrow();
        Assert.Equal(42, receipt.Saved.BatchId);
        Assert.Equal(1, receipt.SeedId);
        Assert.Equal(2, receipt.Sown.Value);
        Assert.Equal("Фаза", receipt.Details.LunarPhase);
        Assert.Equal(Request.Date, receipt.Details.Date);
        Assert.Equal(Request.Source, receipt.Details.Source);
        Assert.Equal(Request.Place, receipt.Details.Place);
        await _session.Received(1).SaveAsync(
            Arg.Is<SowingChange>(change => change.Remaining.Available.Value == 8),
            Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>());
        await _session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Execute_LoadFailure_PreservesReasonWithoutSaving()
    {
        _session.LoadStockAsync(1, SeedUnit.Pieces, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SeedStock>("Пакет удалён"));
        var result = await new SowSeeds(_sessions, _calendar).ExecuteAsync(Request);
        Assert.Equal("Пакет удалён", result.Match(_ => "не ожидалось", error => error));
        await _session.DidNotReceive().SaveAsync(Arg.Any<SowingChange>(), Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>());
        await _session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Execute_ExcessQuantity_DoesNotSaveAndReleasesSession()
    {
        var result = await new SowSeeds(_sessions, _calendar).ExecuteAsync(Request with { Amount = 11 });
        Assert.True(result.IsFailure);
        await _session.DidNotReceive().SaveAsync(Arg.Any<SowingChange>(), Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>());
        await _session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Execute_SaveException_ReleasesSessionAndPreservesException()
    {
        var error = new InvalidOperationException("Хранилище недоступно");
        _session.SaveAsync(Arg.Any<SowingChange>(), Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SavedSowing>(error));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => new SowSeeds(_sessions, _calendar).ExecuteAsync(Request)));
        await _session.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Execute_CancelledBeforeStart_DoesNotOpenSession()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SowSeeds(_sessions, _calendar).ExecuteAsync(Request, cancellation.Token));
        _sessions.DidNotReceive().Create();
    }

    [Fact]
    public async Task Execute_AbsentPlace_PreservesExplicitAbsence()
    {
        var result = await new SowSeeds(_sessions, _calendar).ExecuteAsync(Request with { Place = Option.None<string>() });
        Assert.IsType<Option<string>.None>(result.ValueOrThrow().Details.Place);
    }

    [Fact]
    public async Task Execute_WhitespacePlace_NormalizesToAbsence()
    {
        var result = await new SowSeeds(_sessions, _calendar).ExecuteAsync(Request with { Place = Option.Some("   ") });
        Assert.IsType<Option<string>.None>(result.ValueOrThrow().Details.Place);
    }

    [Fact]
    public async Task Execute_CancelledDuringLoad_ReleasesSessionWithoutSaving()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<Result<SeedStock>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _session.LoadStockAsync(1, SeedUnit.Pieces, cancellation.Token).Returns(pending.Task);
        var execution = new SowSeeds(_sessions, _calendar).ExecuteAsync(Request, cancellation.Token);
        cancellation.Cancel();
        pending.SetCanceled(cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        await _session.Received(1).DisposeAsync();
        await _session.DidNotReceive().SaveAsync(Arg.Any<SowingChange>(), Arg.Any<SowingDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Application_ReferencesOnlyDomainAndRuntime()
        => Assert.All(typeof(SowSeeds).Assembly.GetReferencedAssemblies(), reference =>
            Assert.True(reference.Name == "Bonfire.Domain" || reference.Name!.StartsWith("System", StringComparison.Ordinal)));
}
