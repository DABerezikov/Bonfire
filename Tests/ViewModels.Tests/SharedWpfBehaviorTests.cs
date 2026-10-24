using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Templates;
using Microsoft.Xaml.Behaviors;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class SharedWpfBehaviorTests(WpfRuntime runtime)
{
    [Fact]
    public void CloseWindowCommand_ClosesProvidedWindowWithoutShowingIt() => runtime.Invoke(() =>
    {
        var window = new Window(); var closed = false;
        window.Closed += (_, _) => closed = true;
        ICommand command = new CloseWindow();
        Assert.True(command.CanExecute(window));
        command.Execute(window);
        Assert.True(closed);
        Assert.False(command.CanExecute(null));
    });

    [Fact]
    public void FocusBehavior_SelectsAllTextAndDetachesCleanly() => runtime.Invoke(() =>
    {
        var input = new TextBox { Text = "Название сорта" };
        var behavior = new SelectAllTextOnFocusBehavior();
        Interaction.GetBehaviors(input).Add(behavior);
        input.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, input) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
        Assert.Equal(input.Text, input.SelectedText);
        input.Select(0, 0);
        input.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.GotMouseCaptureEvent });
        Assert.Equal(input.Text, input.SelectedText);
        Interaction.GetBehaviors(input).Remove(behavior);
        input.Select(0, 0);
        input.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, input) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
        Assert.Equal(string.Empty, input.SelectedText);
    });

    [Fact]
    public void StyleBehaviors_AreClonedForEachControl() => runtime.Invoke(() =>
    {
        var prototype = new SelectAllTextOnFocusBehavior();
        var first = new TextBox(); var second = new TextBox();
        BehaviorInStyleAttacher.SetBehaviors(first, new[] { prototype });
        BehaviorInStyleAttacher.SetBehaviors(second, new[] { prototype });
        Assert.NotSame(prototype, Assert.Single(Interaction.GetBehaviors(first)));
        Assert.NotSame(Interaction.GetBehaviors(first)[0], Interaction.GetBehaviors(second)[0]);
        Assert.Null(((IAttachedObject)prototype).AssociatedObject);
        Assert.NotNull(BehaviorInStyleAttacher.GetBehaviors(first));
    });

    [Fact]
    public void SharedRowBehavior_TogglesDetailsAndKeepsOnlyOneOpen() => runtime.Invoke(() =>
    {
        var grid = new DataGrid
        {
            ItemsSource = new[] { "Первый", "Второй" },
            RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed,
            RowDetailsTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='Подробности'/></DataTemplate>")
        };
        var host = new Border { Child = grid };
        grid.EnableRowVirtualization = false;
        var toggle = new DoubleClickBehavior(); var single = new SingleRowDetailsBehavior();
        grid.Columns.Add(new DataGridTextColumn { Binding = new System.Windows.Data.Binding() });
        Interaction.GetBehaviors(grid).Add(toggle); Interaction.GetBehaviors(grid).Add(single);
        WpfRuntime.Layout(host, 600, 400);
        var first = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
        var second = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(1));
        grid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent, Source = first });
        WpfRuntime.Layout(host, 600, 400);
        Assert.Equal(Visibility.Visible, first.DetailsVisibility);
        grid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent, Source = second });
        WpfRuntime.Layout(host, 600, 400);
        Assert.Equal(Visibility.Collapsed, first.DetailsVisibility);
        Assert.Equal(Visibility.Visible, second.DetailsVisibility);
        Interaction.GetBehaviors(grid).Remove(toggle); Interaction.GetBehaviors(grid).Remove(single);
        grid.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent, Source = second });
        Assert.Equal(Visibility.Visible, second.DetailsVisibility);
    });
}
