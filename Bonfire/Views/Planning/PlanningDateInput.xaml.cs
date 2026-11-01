using System;
using System.Windows;
using System.Windows.Controls;
using Bonfire.Models.Planning;

namespace Bonfire.Views.Planning;

/// <summary>Календарь и свободный ввод: неверный текст сохраняется для исправления.</summary>
public partial class PlanningDateInput : UserControl
{
    private bool _synchronizing;
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
        typeof(PlanningDateInput), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (sender, _) => ((PlanningDateInput)sender).SynchronizeCalendar()));

    public PlanningDateInput()
    {
        InitializeComponent();
        SynchronizeCalendar();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void SynchronizeCalendar()
    {
        if (CalendarPicker is null || _synchronizing) return;
        _synchronizing = true;
        try
        {
            CalendarPicker.SelectedDate = PlanningInputParser.Date(Text, "Дата")
                .Match<DateTime?>(date => date.ToDateTime(TimeOnly.MinValue), _ => null);
        }
        finally { _synchronizing = false; }
    }

    private void CalendarDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_synchronizing) return;
        if (CalendarPicker.SelectedDate is { } date)
            SetCurrentValue(TextProperty, PlanningInputParser.Format(DateOnly.FromDateTime(date)));
    }
}
