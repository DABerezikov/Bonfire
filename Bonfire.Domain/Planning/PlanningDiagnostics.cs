namespace Bonfire.Domain.Planning;

public enum PlanningDiagnosticCode
{
    VegetationMissing,
    ManualSowingOutsideWindow,
    ManualTransplantOutsideWindow,
    IncompatibleAge,
    UnknownPrickingOut,
    UnknownHardening,
    StageOrder
}

public sealed record PlanningDiagnostic(PlanningDiagnosticCode Code, string Message);

/// <summary>Возраст может быть отрицательным: это видимый конфликт, а не нулевой возраст.</summary>
public sealed record AgeBounds(int Min, int Max);
