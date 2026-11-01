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

/// <summary>Разность дат по исходному периоду; отрицательная граница сама по себе не означает несовместимость.</summary>
public sealed record AgeBounds(int Min, int Max);
