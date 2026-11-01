using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record PlotDimensions
{
    private PlotDimensions(double widthMeters, double heightMeters, CanvasRectangle canvas)
        => (WidthMeters, HeightMeters, Canvas) = (widthMeters, heightMeters, canvas);

    public double WidthMeters { get; }
    public double HeightMeters { get; }
    public CanvasRectangle Canvas { get; }

    public static Result<PlotDimensions> Create(double widthMeters, double heightMeters, double pixelsPerMeter)
        => double.IsFinite(widthMeters) && widthMeters >= 0 && double.IsFinite(heightMeters) && heightMeters >= 0 &&
           double.IsFinite(pixelsPerMeter) && pixelsPerMeter > 0
            ? CanvasRectangle.Create(0, 0, widthMeters * pixelsPerMeter, heightMeters * pixelsPerMeter)
                .Bind(canvas => Result.Success(new PlotDimensions(widthMeters, heightMeters, canvas)))
            : Result.Failure<PlotDimensions>(
                "Размеры должны быть конечными и неотрицательными, масштаб — конечным и положительным");
}