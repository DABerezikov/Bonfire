using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Bonfire.Infrastructure;

internal sealed class WindowShutdown(Func<Task> stop, Action close, Action<Exception> reportError)
{
    private bool _pending;
    private bool _ready;

    public async Task OnClosingAsync(CancelEventArgs args)
    {
        if (args.Cancel || _ready) return;
        args.Cancel = true;
        if (_pending) return;
        _pending = true;
        try { await stop(); }
        catch (Exception error) { reportError(error); }
        finally
        {
            // Даже синхронная остановка должна выйти из исходного события Closing до нового Close.
            await Task.Yield();
            _ready = true;
            close();
        }
    }
}
