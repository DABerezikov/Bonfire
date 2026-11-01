using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
        var stage = 0;
        Exception failure = null;
        var checkedScreens = new List<string>();
        timer.Tick += (_, _) =>
        {
            if ((DateTime.UtcNow - started).TotalSeconds > 20) { timer.Stop(); app.Shutdown(2); return; }
            if (app.MainWindow is { IsLoaded: true } window)
            {
                reached = true;
                if (++loadedTicks < 3 || closeRequested) return;
                try
                {
                    var model = window.DataContext;
                    switch (stage)
                    {
                        case 0:
                            CheckView(window, "SeedsWindow"); Navigate(model, "ShowPlantingPlanningCommand"); break;
                        case 1:
                            if (!HasRegion(Property(model, "PlantingPlanningViewModel"))) return;
                            CheckView(window, "PlantingPlanningView"); Navigate(model, "ShowHarvestJournalCommand"); break;
                        case 2:
                            if (!HasRegion(Property(model, "HarvestJournalViewModel"))) return;
                            CheckView(window, "HarvestJournalView"); Navigate(model, "ShowPerennialPlanningCommand"); break;
                        case 3:
                            var perennial = Property(model, "PerennialPlanningViewModel");
                            if (!HasRegion(perennial) || (bool)Property(perennial, "IsBusy")) return;
                            CheckView(window, "PerennialPlanningView"); Navigate(model, "ShowLibraryEditorViewModelCommand"); break;
                        case 4:
                            CheckView(window, "LibraryEditorWindow", false);
                            var tab = Descendants(window).OfType<TabControl>().First(x => x.Items.OfType<TabItem>().Any(item => item.Header?.ToString()?.StartsWith("Планирование:") == true));
                            tab.SelectedItem = tab.Items.OfType<TabItem>().Single(item => item.Header?.ToString()?.StartsWith("Планирование:") == true); break;
                        case 5:
                            if (!HasRegion(Property(Property(model, "LibraryEditorViewModel"), "PlanningLibraries"))) return;
                            CheckView(window, "PlanningLibrariesView"); Navigate(model, "ShowSeedViewModelCommand"); break;
                        case 6:
                            CheckView(window, "SeedsWindow"); closeRequested = true; window.Close(); break;
                    }
                    stage++; loadedTicks = 0;
                }
                catch (Exception error) { failure = error; timer.Stop(); app.Shutdown(3); }
            }
        };
        timer.Start();
        var exitCode = app.Run();
        timer.Stop();
        if (!reached || exitCode != 0 || stage < 7) throw new Exception("Окно или новые экраны не прошли startup smoke", failure);
        try { Bonfire.App.Host.Services.CreateScope(); throw new Exception("Host не освобождён"); }
        catch (ObjectDisposedException) { }
        Console.WriteLine($"STARTUP_SMOKE_OK; Database={database}; WindowLoaded={reached}; HostDisposed=True; Screens={string.Join(",", checkedScreens)}");

        void CheckView(Window window, string name, bool checkBindings = true)
        {
            var view = Descendants(window).OfType<FrameworkElement>().Single(x => x.GetType().Name == name);
            if (checkBindings)
                foreach (var box in Descendants(view).OfType<TextBox>().Where(x => x.IsVisible))
                    if (BindingOperations.GetBindingExpression(box, TextBox.TextProperty) is { } binding && binding.Status != BindingStatus.Active)
                        throw new Exception($"{name}: привязка {binding.ParentBinding.Path} имеет статус {binding.Status}");
            checkedScreens.Add(name);
        }
    }
    private static object Property(object model, string name) => model.GetType().GetProperty(name)?.GetValue(model) ?? throw new Exception($"Нет свойства {name}");
    private static bool HasRegion(object model) => Property(model, "Regions") is ICollection { Count: > 0 };
    private static void Navigate(object model, string name)
    { var command = (ICommand)Property(model, name); if (!command.CanExecute(null)) throw new Exception($"Команда {name} недоступна"); command.Execute(null); }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
}
