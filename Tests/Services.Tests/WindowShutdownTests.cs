using System.ComponentModel;
using Bonfire.Infrastructure;

namespace Services.Tests;

public class WindowShutdownTests
{
    [Fact]
    public async Task Closing_PendingShutdown_CancelsCloseUntilStopCompletes()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = false;
        var gate = new WindowShutdown(() => pending.Task, () => closed = true, _ => { });
        var args = new CancelEventArgs();
        var closing = gate.OnClosingAsync(args);
        try { Assert.True(args.Cancel); Assert.False(closed); Assert.False(closing.IsCompleted); }
        finally { pending.TrySetResult(); }
        await closing;
        Assert.True(closed);
    }

    [Fact]
    public async Task Closing_UserRefusedDiscard_DoesNotStopOrClose()
    {
        var stopped = false;
        var closed = false;
        var gate = new WindowShutdown(() => { stopped = true; return Task.CompletedTask; }, () => closed = true, _ => { });
        await gate.OnClosingAsync(new CancelEventArgs(true));
        Assert.False(stopped);
        Assert.False(closed);
    }

    [Fact]
    public async Task Closing_RepeatedClose_StopsAndClosesOnce()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stops = 0;
        var closes = 0;
        var gate = new WindowShutdown(() => { stops++; return pending.Task; }, () => closes++, _ => { });
        var first = gate.OnClosingAsync(new CancelEventArgs());
        var repeated = new CancelEventArgs();
        await gate.OnClosingAsync(repeated);
        pending.SetResult();
        await first;
        var final = new CancelEventArgs();
        await gate.OnClosingAsync(final);
        Assert.True(repeated.Cancel);
        Assert.False(final.Cancel);
        Assert.Equal(1, stops);
        Assert.Equal(1, closes);
    }

    [Fact]
    public async Task Closing_StopError_ReportsItAndStillCloses()
    {
        var error = new InvalidOperationException("Ошибка остановки");
        var errors = new List<Exception>();
        var closed = false;
        var gate = new WindowShutdown(() => Task.FromException(error), () => closed = true, errors.Add);
        await gate.OnClosingAsync(new CancelEventArgs());
        Assert.Same(error, Assert.Single(errors));
        Assert.True(closed);
    }
}
