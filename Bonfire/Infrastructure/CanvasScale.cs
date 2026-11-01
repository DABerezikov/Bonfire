using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Bonfire.Infrastructure;

internal static class CanvasScale
{
    public static double Get(UIElement element)
    {
        try
        {
            DependencyObject current = element;
            while (true)
            {
                var parent = VisualTreeHelper.GetParent(current);
                switch (parent)
                {
                    case null:
                        return 1.0;
                    case Canvas canvas:
                    {
                        var window = Window.GetWindow(canvas);
                        if (window is null) return 1.0;
                        var transform = canvas.TransformToAncestor(window);
                        var origin = transform.Transform(new Point(0, 0));
                        var unitX = transform.Transform(new Point(1, 0));
                        var scale = unitX.X - origin.X;
                        return scale > 0 ? scale : 1.0;
                    }
                    default:
                        current = parent;
                        break;
                }
            }
        }
        catch
        {
            return 1.0;
        }
    }
}