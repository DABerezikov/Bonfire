using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.ViewModels;

namespace Bonfire.Views.GardenPlan;

public partial class GardenPlanView : UserControl
{
    private readonly HashSet<ValidationError> _elementEditorErrors = [];
    private readonly HashSet<ValidationError> _gardenEditorErrors = [];
    private readonly HashSet<ValidationError> _greenhouseEditorErrors = [];

    public GardenPlanView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnElementEditorValidationError(object sender, ValidationErrorEventArgs e)
    {
        var hasErrors = UpdateValidationErrors(_elementEditorErrors, e);
        if (DataContext is GardenPlanViewModel vm) vm.ElementEditorHasBindingErrors = hasErrors;
    }

    private void OnGardenEditorValidationError(object sender, ValidationErrorEventArgs e)
    {
        var hasErrors = UpdateValidationErrors(_gardenEditorErrors, e);
        if (DataContext is GardenPlanViewModel vm) vm.GardenEditorHasBindingErrors = hasErrors;
    }

    private void OnGreenhouseEditorValidationError(object sender, ValidationErrorEventArgs e)
    {
        var hasErrors = UpdateValidationErrors(_greenhouseEditorErrors, e);
        if (DataContext is GardenPlanViewModel vm) vm.GreenhouseEditorHasBindingErrors = hasErrors;
    }

    private static bool UpdateValidationErrors(HashSet<ValidationError> errors, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) errors.Add(e.Error);
        else errors.Remove(e.Error);
        CommandManager.InvalidateRequerySuggested();
        return errors.Count > 0;
    }

    private void OnGardenEditorUnloaded(object sender, RoutedEventArgs e)
    {
        _gardenEditorErrors.Clear();
        if (DataContext is GardenPlanViewModel vm) vm.GardenEditorHasBindingErrors = false;
    }

    private void OnGreenhouseEditorUnloaded(object sender, RoutedEventArgs e)
    {
        _greenhouseEditorErrors.Clear();
        if (DataContext is GardenPlanViewModel vm) vm.GreenhouseEditorHasBindingErrors = false;
    }

    private void OnElementEditorUnloaded(object sender, RoutedEventArgs e)
    {
        _elementEditorErrors.Clear();
        if (DataContext is GardenPlanViewModel vm) vm.ElementEditorHasBindingErrors = false;
    }

    private void GardenScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (DataContext is GardenPlanViewModel vm)
                vm.GardenZoom += e.Delta > 0 ? 0.1 : -0.1;
            e.Handled = true;
        }
    }

    /// <summary>Клик по пустому месту холста участка — взять фокус для Escape.</summary>
    private void GardenCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        GardenCanvas.Focus();
    }

    /// <summary>Клик по пустому месту внутреннего холста теплицы — взять фокус для Escape.</summary>
    private void GreenhouseCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        GreenhouseCanvas.Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is GardenPlanViewModel vm)
        {
            vm.DeselectElementCommand.Execute(null);
            e.Handled = true;
        }
    }
}