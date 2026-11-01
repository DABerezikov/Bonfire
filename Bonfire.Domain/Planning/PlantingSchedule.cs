using System.Collections.Immutable;
using System.Diagnostics;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Bonfire.Domain.Planning;

public sealed record CandidateReadiness(
    DateWindow Emergence,
    DateWindow Readiness,
    Option<DateWindow> AllowedTransplant,
    AgeBounds AgeAtTarget);

/// <summary>Прогноз по введённым диапазонам. Ручные даты не переписываются расчётом.</summary>
public sealed record PlantingSchedule(
    DateWindow Sowing,
    Option<DateWindow> ResilientSowing,
    DateWindow MeanReference,
    DateWindow Emergence,
    Option<DateWindow> Maturity,
    Option<DateWindow> Transplant,
    Option<DateWindow> PrickingOut,
    Option<DateWindow> HardeningStart,
    ImmutableArray<PlanningDiagnostic> Diagnostics)
{
    public static Result<PlantingSchedule> Calculate(GrowingMethod method, EffectiveGrowingProfile profile,
        PlanningChoices choices, Option<EffectivePlanningExtras> extras)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(extras);
        return method switch
        {
            GrowingMethod.DirectSow direct => choices.TransplantDate is Option<DateOnly>.Some
                ? Result.Failure<PlantingSchedule>("Дата высадки неприменима к прямому посеву")
                : Build(direct.SowingWindow, Option.None<DateWindow>(),
                    direct.SowingWindow.Shift(Mean(profile.Germination.Value), Mean(profile.Germination.Value)),
                    Option.None<DateWindow>(), profile, choices, extras),
            GrowingMethod.Transplant transplant => profile.SeedlingAge.Match(age =>
                    CalculateTransplant(transplant.TransplantWindow, profile, age.Value, choices, extras),
                () => Result.Failure<PlantingSchedule>("Не задан возраст рассады от всходов")),
            _ => throw new UnreachableException()
        };
    }

    private static Result<PlantingSchedule> CalculateTransplant(DateWindow target, EffectiveGrowingProfile profile,
        DayRange age, PlanningChoices choices, Option<EffectivePlanningExtras> extras)
    {
        var g = profile.Germination.Value;
        return target.Shift(-(long)g.Max - age.Max, -(long)g.Min - age.Min).Bind(sowing =>
        {
            // При допустимом possible обе границы resilient лежат в календаре.
            var resilientStart = target.Start.DayNumber - (long)g.Min - age.Max;
            var resilientEnd = target.End.DayNumber - (long)g.Max - age.Min;
            var resilient = resilientStart > resilientEnd
                ? Option.None<DateWindow>()
                : Option.Some(DateWindow.Create(DateOnly.FromDayNumber((int)resilientStart),
                    DateOnly.FromDayNumber((int)resilientEnd)).ValueOrThrow());
            var mean = Mean(g) + Mean(age);
            return Build(sowing, resilient, target.Shift(-mean, -mean), Option.Some(target), profile, choices, extras);
        });
    }

    private static Result<PlantingSchedule> Build(DateWindow sowing, Option<DateWindow> resilient,
        Result<DateWindow> mean, Option<DateWindow> transplant, EffectiveGrowingProfile profile,
        PlanningChoices choices, Option<EffectivePlanningExtras> extras)
    {
        var g = profile.Germination.Value;
        var selectedSowing =
            choices.SowingDate.Match(date => DateWindow.Create(date, date).ValueOrThrow(), () => sowing);
        var selectedTransplant = transplant.Map(target => choices.TransplantDate.Match(
            date => DateWindow.Create(date, date).ValueOrThrow(), () => target));
        return mean.Bind(meanWindow => selectedSowing.Shift(g.Min, g.Max).Bind(emergence =>
            ShiftOptional(profile.Vegetation.Map(x => x.Value), emergence).Bind(maturity =>
            {
                var diagnostics = ImmutableArray.CreateBuilder<PlanningDiagnostic>();
                if (maturity is Option<DateWindow>.None)
                    diagnostics.Add(new PlanningDiagnostic(PlanningDiagnosticCode.VegetationMissing,
                        "Не задан срок вегетации от всходов; прогноз зрелости недоступен"));
                if (choices.SowingDate is Option<DateOnly>.Some sow && !sowing.Contains(sow.Value))
                    diagnostics.Add(new PlanningDiagnostic(PlanningDiagnosticCode.ManualSowingOutsideWindow,
                        "Ручная дата посева находится вне расчётного окна"));
                if (choices.TransplantDate is Option<DateOnly>.Some plant
                    && transplant.Match(window => !window.Contains(plant.Value), () => false))
                    diagnostics.Add(new PlanningDiagnostic(PlanningDiagnosticCode.ManualTransplantOutsideWindow,
                        "Ручная дата высадки находится вне заданного периода"));
                if (transplant is Option<DateWindow>.None)
                    return Result.Success(new PlantingSchedule(sowing, resilient, meanWindow, emergence, maturity,
                        transplant, Option.None<DateWindow>(), Option.None<DateWindow>(), diagnostics.ToImmutable()));

                var target = selectedTransplant.Match(value => value, () => throw new UnreachableException());
                var age = profile.SeedlingAge.Match(value => value.Value, () => throw new UnreachableException());
                return emergence.Shift(age.Min, age.Max).Bind(readiness =>
                {
                    if (readiness.Intersect(target) is Option<DateWindow>.None)
                        diagnostics.Add(new PlanningDiagnostic(PlanningDiagnosticCode.IncompatibleAge,
                            "Посев и высадка несовместимы с заданным возрастом рассады"));
                    var pricking = extras.Match(value => value.PrickingOut,
                        () => new OperationPlan<DayRange>.Unknown());
                    var hardening = extras.Match(value => value.Hardening, () => new OperationPlan<DayRange>.Unknown());
                    return CalculateOperation(pricking, emergence, false, diagnostics,
                            PlanningDiagnosticCode.UnknownPrickingOut)
                        .Bind(p => CalculateOperation(hardening, target, true, diagnostics,
                                PlanningDiagnosticCode.UnknownHardening)
                            .Bind(h =>
                            {
                                if (p.Match(window => window.End > target.End || window.Start < emergence.Start,
                                        () => false)
                                    || h.Match(window => window.Start < emergence.Start, () => false)
                                    || p.Match(pw => h.Match(hw => pw.End > hw.End, () => false), () => false))
                                    diagnostics.Add(new PlanningDiagnostic(PlanningDiagnosticCode.StageOrder,
                                        "Диапазоны пикировки/закаливания выходят за порядок всходы → подготовка → высадка"));
                                return Result.Success(new PlantingSchedule(sowing, resilient, meanWindow, emergence,
                                    maturity,
                                    selectedTransplant, p, h, diagnostics.ToImmutable()));
                            }));
                });
            })));
    }

    private static Result<Option<DateWindow>> ShiftOptional(Option<DayRange> days, DateWindow window)
        => days.Match(range => window.Shift(range.Min, range.Max).Bind(value => Result.Success(Option.Some(value))),
            () => Result.Success(Option.None<DateWindow>()));

    private static Result<Option<DateWindow>> CalculateOperation(OperationPlan<DayRange> operation, DateWindow basis,
        bool backwards, ImmutableArray<PlanningDiagnostic>.Builder diagnostics, PlanningDiagnosticCode unknownCode)
    {
        switch (operation)
        {
            case OperationPlan<DayRange>.Enabled enabled:
                var days = enabled.Setting.Value;
                return basis.Shift(backwards ? -(long)days.Max : days.Min, backwards ? -(long)days.Min : days.Max)
                    .Bind(value => Result.Success(Option.Some(value)));
            case OperationPlan<DayRange>.Unknown:
                diagnostics.Add(new PlanningDiagnostic(unknownCode,
                    unknownCode == PlanningDiagnosticCode.UnknownPrickingOut
                        ? "Уточните необходимость пикировки"
                        : "Уточните необходимость закаливания"));
                return Result.Success(Option.None<DateWindow>());
            case OperationPlan<DayRange>.Disabled:
                return Result.Success(Option.None<DateWindow>());
            default:
                throw new UnreachableException();
        }
    }

    public static Result<CandidateReadiness> EvaluateCandidate(DateOnly sowingDate, DateWindow target,
        DayRange germination, DayRange age)
        => DateWindow.Create(sowingDate, sowingDate).Bind(sowing => sowing.Shift(germination.Min, germination.Max)
            .Bind(emergence => emergence.Shift(age.Min, age.Max).Bind(readiness =>
                Result.Success(new CandidateReadiness(emergence, readiness, readiness.Intersect(target),
                    new AgeBounds(target.Start.DayNumber - sowingDate.DayNumber - germination.Max,
                        target.End.DayNumber - sowingDate.DayNumber - germination.Min))))));

    private static long Mean(DayRange range) => (long)decimal.Ceiling(((decimal)range.Min + range.Max) / 2m);
}