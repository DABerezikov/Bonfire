using System;
using System.Windows;

namespace Bonfire.Infrastructure;

/// <summary>
/// Константы отображения огорода, зависящие от разрешения экрана.
/// </summary>
public static class CanvasConstants
{
    /// <summary>
    /// Масштаб холста: пикселей (DIPs) на метр.
    /// Базовое значение 150 пкс/м откалибровано под ширину 1920 DIPs (1920×1080 @ 100%).
    /// На других разрешениях масштабируется линейно по ширине экрана.
    /// </summary>
    public static readonly double PixelsPerMeter =
        Math.Round(150.0 * (SystemParameters.PrimaryScreenWidth / 1920.0));

    /// <summary>
    /// Порог компактного режима элемента: если отображаемая сторона (px × zoom) меньше
    /// этого значения — показывается только цветная точка вместо текста.
    /// Базовое значение 90 пкс откалибровано под 1920 DIPs.
    /// </summary>
    public static readonly double CompactThreshold =
        Math.Round(90.0 * (SystemParameters.PrimaryScreenWidth / 1920.0));
}
