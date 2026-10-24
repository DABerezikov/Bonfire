using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ViewModels.Tests;

[CollectionDefinition("WPF runtime", DisableParallelization = true)]
public class WpfRuntimeCollection : ICollectionFixture<WpfRuntime>;

// Единственный Application на изолированном STA-потоке. Окна не показываются, App.OnStartup не вызывается.
public sealed class WpfRuntime : IDisposable
{
    private readonly Thread _thread;
    private readonly Dispatcher _dispatcher;

    public WpfRuntime()
    {
        var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        _thread = new Thread(() =>
        {
            try
            {
                var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                foreach (var resource in new[]
                {
                    "Infrastructure/Commands/Commands.xaml", "Resources/Base/Brushes.xaml", "Resources/Base/Converters.xaml",
                    "Templates/ModelMainView.xaml", "Templates/DataGridStyle.xaml", "Templates/SeedsViewStyle.xaml"
                })
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri("pack://application:,,,/Bonfire;component/" + resource) });
                ready.SetResult(Dispatcher.CurrentDispatcher);
                Dispatcher.Run();
            }
            catch (Exception error) { ready.TrySetException(error); }
        }) { IsBackground = true, Name = "Bonfire WPF tests" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _dispatcher = ready.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    }

    public void Invoke(Action action) => _dispatcher.Invoke(action);

    public static void Layout(FrameworkElement control, double width = 1920, double height = 900)
    {
        control.Measure(new Size(width, height));
        control.Arrange(new Rect(0, 0, width, height));
        control.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    public static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    public void Dispose()
    {
        _dispatcher.InvokeShutdown();
        if (!_thread.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("WPF dispatcher did not stop");
    }
}
