using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Bonfire.Infrastructure;
using Bonfire.Models;

namespace Bonfire.Views.GardenPlan;

public partial class GardenElementControl : UserControl
{
    // Позиция и размер до начала drag — восстанавливаем при обнаружении коллизии
    private double _preDragX, _preDragY;
    private double _preDragW, _preDragH;

    public GardenElementControl()
    {
        InitializeComponent();
    }

    // Начало любого drag (перемещение или ресайз):
    //   1. Берём клавиатурный фокус → GardenPlanView.PreviewKeyDown работает для Escape
    //   2. Сохраняем текущую позицию/размер для возможного отката
    public void OnDragStarted(object sender, DragStartedEventArgs e)
    {
        Focus();
        if (DataContext is GardenElementFromViewModel vm)
        {
            _preDragX = vm.X;
            _preDragY = vm.Y;
            _preDragW = vm.Width;
            _preDragH = vm.Height;
        }
    }

    // Перемещение: обновляем позицию без проверки коллизий (проверка — при отпускании)
    public void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;

        double maxX = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.Width  : double.MaxValue;
        double maxY = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Height : double.MaxValue;

        vm.X = Math.Max(0, Math.Min(vm.X + e.HorizontalChange, maxX));
        vm.Y = Math.Max(0, Math.Min(vm.Y + e.VerticalChange,   maxY));
    }

    // Отпускание после перемещения: проверяем коллизию, при необходимости откатываем.
    // Обработчик зарегистрирован атрибутом XAML → выполняется ДО EventTrigger SaveElementPositionCommand,
    // поэтому команда сохранит уже скорректированную позицию.
    private void OnMoveDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;
        if (CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.Width, vm.Height, vm))
        {
            vm.X = _preDragX;
            vm.Y = _preDragY;
        }
    }

    // Изменение размера: обновляем размер без проверки коллизий
    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;

        double maxW = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.X : double.MaxValue;
        double maxH = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Y : double.MaxValue;

        vm.Width  = Math.Max(60, Math.Min(vm.Width  + e.HorizontalChange, maxW));
        vm.Height = Math.Max(40, Math.Min(vm.Height + e.VerticalChange,   maxH));
    }

    // Отпускание после ресайза: проверяем коллизию, откатываем размер если нужно.
    private void OnResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;
        if (CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.Width, vm.Height, vm))
        {
            vm.Width  = _preDragW;
            vm.Height = _preDragH;
        }
    }
}
