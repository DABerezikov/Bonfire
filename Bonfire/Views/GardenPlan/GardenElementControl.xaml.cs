using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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
        if (DataContext is not GardenElementFromViewModel vm || vm.IsLocked) return;

        const double snap = 4; // 0.1 м = 4 пкс
        double scale = GetCanvasScale(this);
        double maxX = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.Width  : double.MaxValue;
        double maxY = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Height : double.MaxValue;

        vm.X = Math.Round(Math.Max(0, Math.Min(vm.X + e.HorizontalChange / scale, maxX)) / snap) * snap;
        vm.Y = Math.Round(Math.Max(0, Math.Min(vm.Y + e.VerticalChange   / scale, maxY)) / snap) * snap;
    }

    // Отпускание после перемещения: проверяем коллизию, при необходимости откатываем.
    // Обработчик зарегистрирован атрибутом XAML → выполняется ДО EventTrigger SaveElementPositionCommand,
    // поэтому команда сохранит уже скорректированную позицию.
    private void OnMoveDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;
        bool wasColliding = CollisionHelper.CollidesWithSiblings(_preDragX, _preDragY, vm.Width, vm.Height, vm);
        bool isColliding  = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.Width, vm.Height, vm);
        // Откатываем только если старт был чистым, а новая позиция — нет
        if (isColliding && !wasColliding)
        {
            vm.X = _preDragX;
            vm.Y = _preDragY;
        }
    }

    // Изменение размера: обновляем размер без проверки коллизий
    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm || vm.IsLocked) return;

        double scale = GetCanvasScale(this);
        double maxW = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.X : double.MaxValue;
        double maxH = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Y : double.MaxValue;

        vm.Width  = Math.Round(Math.Max(20, Math.Min(vm.Width  + e.HorizontalChange / scale, maxW)));
        vm.Height = Math.Round(Math.Max(20, Math.Min(vm.Height + e.VerticalChange   / scale, maxH)));
    }

    // Масштаб Viewbox: Thumb.DragDelta возвращает дельту в координатах окна.
    // Делим на scale чтобы получить дельту в пикселях Canvas.
    private static double GetCanvasScale(UIElement element)
    {
        try
        {
            DependencyObject current = element;
            while (true)
            {
                var parent = VisualTreeHelper.GetParent(current);
                if (parent is null) return 1.0;
                if (parent is Canvas canvas)
                {
                    var window = Window.GetWindow(canvas);
                    if (window is null) return 1.0;
                    var t      = canvas.TransformToAncestor(window);
                    var origin = t.Transform(new Point(0, 0));
                    var unitX  = t.Transform(new Point(1, 0));
                    var scale  = unitX.X - origin.X;
                    return scale > 0 ? scale : 1.0;
                }
                current = parent;
            }
        }
        catch { return 1.0; }
    }

    // Отпускание после ресайза: проверяем коллизию, откатываем размер если нужно.
    private void OnResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel vm) return;
        bool wasColliding = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, _preDragW, _preDragH, vm);
        bool isColliding  = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.Width, vm.Height, vm);
        if (isColliding && !wasColliding)
        {
            vm.Width  = _preDragW;
            vm.Height = _preDragH;
        }
    }
}
