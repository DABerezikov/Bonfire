using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "isolated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var database = Path.Combine(folder, "smoke.db");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), JsonSerializer.Serialize(new
        {
            Database = new { Type = "SQLite", ConnectionStrings = new { SQLite = $"Data Source={database};Pooling=False" } },
            Logging = new { LogLevel = new { Default = "Warning" } }
        }));
        Environment.CurrentDirectory = folder;
        var app = new Bonfire.App();
        app.InitializeComponent();
        var hidden = new Style(typeof(Window));
        hidden.Setters.Add(new Setter(UIElement.OpacityProperty, 0d));
        hidden.Setters.Add(new Setter(Window.ShowInTaskbarProperty, false));
        app.Resources[typeof(Bonfire.MainWindow)] = hidden;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        var started = DateTime.UtcNow;
        var loadedTicks = 0;
        var reached = false;
        var closeRequested = false;
        timer.Tick += (_, _) =>
        {
            if ((DateTime.UtcNow - started).TotalSeconds > 20) { timer.Stop(); app.Shutdown(2); return; }
            if (app.MainWindow is { IsLoaded: true })
            {
                reached = true;
                if (++loadedTicks >= 5 && !closeRequested) { closeRequested = true; app.MainWindow.Close(); }
            }
        };
        timer.Start();
        var exitCode = app.Run();
        timer.Stop();
        if (!reached || exitCode != 0) throw new Exception("Окно не запустилось или приложение завершилось с ошибкой");
        try { Bonfire.App.Host.Services.CreateScope(); throw new Exception("Host не освобождён"); }
        catch (ObjectDisposedException) { }
        Console.WriteLine($"STARTUP_SMOKE_OK; Database={database}; WindowLoaded={reached}; HostDisposed=True");
    }
}
