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

    /// <summary>
    /// Адаптивный размер шрифта при zoom &lt; 1.0.
    /// Чем меньше базовый размер — тем больший буфер компенсации:
    ///   base=11 → +22.5%,  base=10 → +25%,  base=9 → +27.5%,  base=8 → +30%.
    /// Формула буфера: factor = 1.25 + (10 − base) × 0.025
    /// При zoom ≥ 1.0 возвращает basePx без изменений.
    /// </summary>
    public static double AdaptiveFont(double basePx, double zoom)
    {
        if (zoom >= 1.0) return basePx;
        double factor = 1.25 + (10.0 - basePx) * 0.025;
        return Math.Round(Math.Min(basePx * factor / zoom, basePx * 2.0), 1);
    }
}
