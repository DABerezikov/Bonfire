using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public enum GrowingLocation { OpenGround, Greenhouse, Windowsill }
public enum PlantPlacement { InGround, Container }
public enum LunarGroup { Unspecified, AboveGround, RootCrop }

public sealed record GrowingConditions
{
    public GrowingLocation Location { get; }
    public PlantPlacement Placement { get; }
    private GrowingConditions(GrowingLocation location, PlantPlacement placement)
        => (Location, Placement) = (location, placement);

    public static Result<GrowingConditions> Create(GrowingLocation location, PlantPlacement placement)
        => Enum.IsDefined(location) && Enum.IsDefined(placement)
            && (location != GrowingLocation.Windowsill || placement == PlantPlacement.Container)
            ? Result.Success(new GrowingConditions(location, placement))
            : Result.Failure<GrowingConditions>("Недопустимое сочетание места и размещения растений");
}

public abstract record GrowingMethod
{
    private GrowingMethod() { }

    public sealed record Transplant : GrowingMethod
    {
        public DateWindow TransplantWindow { get; }
        public Transplant(DateWindow transplantWindow)
        {
            ArgumentNullException.ThrowIfNull(transplantWindow);
            TransplantWindow = transplantWindow;
        }
    }

    public sealed record DirectSow : GrowingMethod
    {
        public DateWindow SowingWindow { get; }
        public DirectSow(DateWindow sowingWindow)
        {
            ArgumentNullException.ThrowIfNull(sowingWindow);
            SowingWindow = sowingWindow;
        }
    }
}
