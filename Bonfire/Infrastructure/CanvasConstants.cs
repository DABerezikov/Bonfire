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
}
