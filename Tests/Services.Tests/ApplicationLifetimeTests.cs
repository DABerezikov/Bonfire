using Bonfire.Infrastructure;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace Services.Tests;

public class ApplicationLifetimeTests
{
    [Fact]
    public async Task Start_PendingInitialization_DoesNotStartHostOrBlockCaller()
    {
        var host = Substitute.For<IHost>();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var lifetime = new ApplicationLifetime(host, _ => ready.Task);
        var start = lifetime.StartAsync();
        try
        {
            Assert.False(start.IsCompleted);
            await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        }
        finally { ready.TrySetResult(); }
        await start.WaitAsync(TimeSpan.FromSeconds(5));
        await host.Received(1).StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAndDispose_RepeatedCalls_RunEachPhaseOnce()
    {
        var host = Substitute.For<IHost>();
        var initializations = 0;
        var lifetime = new ApplicationLifetime(host, _ => { initializations++; return Task.CompletedTask; });
        await lifetime.StartAsync();
        await lifetime.StartAsync();
        await lifetime.DisposeAsync();
        await lifetime.DisposeAsync();
        Assert.Equal(1, initializations);
        await host.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        host.Received(1).Dispose();
    }

    [Fact]
    public async Task Dispose_PendingHostStop_WaitsBeforeDisposal()
    {
        var host = Substitute.For<IHost>();
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.StopAsync(Arg.Any<CancellationToken>()).Returns(stopped.Task);
        var lifetime = new ApplicationLifetime(host, _ => Task.CompletedTask);
        await lifetime.StartAsync();
        var dispose = lifetime.DisposeAsync().AsTask();
        try
        {
            Assert.False(dispose.IsCompleted);
            host.DidNotReceive().Dispose();
        }
        finally { stopped.TrySetResult(); }
        await dispose.WaitAsync(TimeSpan.FromSeconds(5));
        host.Received(1).Dispose();
    }

    [Fact]
    public async Task Start_InitializationFailure_PreservesErrorAndAllowsCleanup()
    {
        var host = Substitute.For<IHost>();
        var error = new InvalidOperationException("Ошибка миграции");
        var lifetime = new ApplicationLifetime(host, _ => Task.FromException(error));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.StartAsync()));
        await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await lifetime.DisposeAsync();
        host.Received(1).Dispose();
    }

    [Fact]
    public async Task Dispose_DuringInitialization_CancelsItAndReleasesHost()
    {
        var host = Substitute.For<IHost>();
        var lifetime = new ApplicationLifetime(host, token => Task.Delay(Timeout.Infinite, token));
        var start = lifetime.StartAsync();
        await lifetime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        host.Received(1).Dispose();
    }

    [Fact]
    public async Task Dispose_StopFailure_StillDisposesHost()
    {
        var host = Substitute.For<IHost>();
        host.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Ошибка остановки")));
        var lifetime = new ApplicationLifetime(host, _ => Task.CompletedTask);
        await lifetime.StartAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.DisposeAsync().AsTask());
        host.Received(1).Dispose();
    }

    [Fact]
    public async Task Dispose_AsyncDisposableHost_AwaitsAsyncDisposalInsteadOfSyncDisposal()
    {
        var host = Substitute.For<IHost, IAsyncDisposable>();
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ((IAsyncDisposable)host).DisposeAsync().Returns(new ValueTask(disposed.Task));
        var lifetime = new ApplicationLifetime(host, _ => Task.CompletedTask);
        await lifetime.StartAsync();
        var cleanup = lifetime.DisposeAsync().AsTask();
        try { Assert.False(cleanup.IsCompleted); }
        finally { disposed.TrySetResult(); }
        await cleanup;
        host.DidNotReceive().Dispose();
    }
}
