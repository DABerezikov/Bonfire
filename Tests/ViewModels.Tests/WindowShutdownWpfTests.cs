using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using Bonfire.Infrastructure;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class WindowShutdownWpfTests(WpfRuntime runtime)
{
    [Fact]
    public void Closing_ImmediateStop_DefersFinalCloseUntilOriginalEventReturns() => runtime.Invoke(() =>
    {
        var insideEvent = true;
        var closedInsideEvent = false;
        var closed = false;
        var gate = new WindowShutdown(() => Task.CompletedTask, () =>
        {
            closedInsideEvent = insideEvent;
            closed = true;
        }, _ => { });
        var completion = gate.OnClosingAsync(new CancelEventArgs());
        insideEvent = false;
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.True(completion.IsCompletedSuccessfully);
        Assert.True(closed);
        Assert.False(closedInsideEvent);
    });
}
