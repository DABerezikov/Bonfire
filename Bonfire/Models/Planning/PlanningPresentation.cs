using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Models.Planning;

public sealed record PlanningLabel<T>(T Value, string Label);

public static class PlanningLookups
{
    public static ImmutableArray<PlanningLabel<int>> LifeCycles { get; } =
        [new(-1, "Наследовать / неизвестно"), new(0, "Однолетнее"), new(1, "Двулетнее"), new(2, "Многолетнее")];
    public static ImmutableArray<PlanningLabel<HarvestPlanningMode>> HarvestModes { get; } =
        [new(HarvestPlanningMode.ManualPlants, "Растения вручную"), new(HarvestPlanningMode.BedCapacity, "По вместимости"),
         new(HarvestPlanningMode.TargetPerPlant, "Цель кг: кг/растение"), new(HarvestPlanningMode.TargetPerM2, "Цель кг: кг/м²")];
    public static ImmutableArray<PlanningLabel<GrowingLocation>> Locations { get; } =
        [new(GrowingLocation.OpenGround, "Открытый грунт"), new(GrowingLocation.Greenhouse, "Теплица"), new(GrowingLocation.Windowsill, "Подоконник")];
    public static ImmutableArray<PlanningLabel<PlantPlacement>> Placements { get; } =
        [new(PlantPlacement.InGround, "В грунте"), new(PlantPlacement.Container, "Контейнер")];
    public static ImmutableArray<PlanningLabel<int>> Operations { get; } = [new(0, "Не задано / наследовать"), new(1, "Не требуется"), new(2, "Требуется")];
    public static ImmutableArray<PlanningLabel<int>> LunarGroups { get; } =
        [new(-1, "Не задано / наследовать"), new(0, "Без лунной группы"), new(1, "Надземная часть — растущая"), new(2, "Корнеплоды — убывающая")];
    public static ImmutableArray<PlanningLabel<RegionalWindowPurpose>> Purposes { get; } =
        [new(RegionalWindowPurpose.Transplanting, "Высадка рассады"), new(RegionalWindowPurpose.DirectSowing, "Прямой посев")];
}

public sealed record PlannedBatchListItem(StoredPlannedBatchFromViewModel Snapshot)
{
    public int Id => Snapshot.Id;
    public string Name => Snapshot.Document.References.CultureName + " / " + Snapshot.Document.References.SortName;
    public string Method => Snapshot.Document.Batch.Method is GrowingMethod.DirectSow ? "Прямой посев" : "Рассада";
    public string Place => PlanningPresentation.Place(Snapshot.Document.Batch.Conditions);
    public int Plants => Snapshot.Document.Batch.TargetPlants.Value;
    public string Seeds => Snapshot.Document.Quantity.Match(value => value.Seeds.Value.ToString(), () => "Не рассчитано");
    public string Sowing => Snapshot.Document.Batch.Choices.SowingDate.Match(PlanningInputParser.Format,
        () => PlanningPresentation.Window(Snapshot.Document.Preview.Agronomy.Sowing));
}

public sealed record SowingCandidateItem(SowingCandidate Candidate)
{
    public string Label => PlanningInputParser.Format(Candidate.Date)
        + (Candidate.ShiftDays == 0 ? Candidate.InsideResilientWindow ? " — устойчивое окно" : "" : $" — сдвиг {Candidate.ShiftDays:+0;-0} дн.");
    public string Details => Candidate.Readiness.Match(value => $"Возраст на исходную высадку: {value.AgeAtTarget.Min}–{value.AgeAtTarget.Max} дн.",
        () => "Всходы: " + PlanningPresentation.Window(Candidate.Preview.Emergence)
            + Candidate.Preview.Maturity.Match(value => "; зрелость: " + PlanningPresentation.Window(value), () => ""));
}

public sealed record RegionalWindowItem(RegionalPlantingWindow Reference, int Year)
{
    public string Label => (Reference.Purpose == RegionalWindowPurpose.DirectSowing ? "Прямой посев" : "Высадка рассады")
        + " · " + PlanningPresentation.Place(Reference.Conditions) + ": "
        + Reference.Window.Project(Year).Match(PlanningPresentation.Window, error => error);
    public string Source => Reference.Source.Match(value => "Источник: " + value, () => "Источник не указан")
        + Reference.Note.Match(value => "; " + value, () => "");
}

public static class PlanningPresentation
{
    public static string Period(string title, SeasonalPeriod period) => title + ": " + (period switch
    {
        SeasonalPeriod.NotExpected value => "не ожидается — " + value.Reason,
        SeasonalPeriod.Unknown value => "неизвестно — " + value.Reason,
        SeasonalPeriod.Known value => value.Period.Start.Match(x => "начало " + Window(x) + "; ", () => "")
            + value.Period.End.Match(x => "окончание " + Window(x) + "; ", () => "")
            + value.Period.Envelope.Match(x => "оболочка " + Window(x) + "; ", () => "длительность не определена; ")
            + (value.Period.Origin switch { GrowthPeriodOrigin.Manual => "вручную", GrowthPeriodOrigin.FromEmergence => "от всходов", _ => "ежегодная справка" })
            + ". Оболочка не означает непрерывное цветение/плодоношение всех растений.",
        _ => throw new InvalidOperationException("Неизвестное состояние периода")
    });
    public static string Place(GrowingConditions conditions) => (conditions.Location switch
    {
        GrowingLocation.OpenGround => "Открытый грунт", GrowingLocation.Greenhouse => "Теплица", GrowingLocation.Windowsill => "Подоконник", _ => "Неизвестное место"
    }) + (conditions.Placement == PlantPlacement.Container ? " / контейнер" : "");
    public static string Window(DateWindow value) => value.Start == value.End ? PlanningInputParser.Format(value.Start)
        : PlanningInputParser.Format(value.Start) + " – " + PlanningInputParser.Format(value.End);
    public static string Source(ParameterSource source) => source switch { ParameterSource.Culture => "культура", ParameterSource.Sort => "сорт", ParameterSource.Batch => "партия", _ => "неизвестно" };

    public static string Describe(PlanningPreviewFromViewModel preview)
    {
        var text = new StringBuilder();
        foreach (var missing in preview.MissingInputs) text.AppendLine(missing);
        if (preview.Document is not Option<PlanningBatchDocument>.Some present) return text.ToString();
        var document = present.Value;
        text.AppendLine(Harvest(document));
        document.Settings.Extras.Match(extras => extras.LifeCycle.Match(life =>
        {
            text.AppendLine("Жизненный цикл: " + Source(life.Source));
            GrowthPeriods.FirstYear(life.Value, document.Preview.Agronomy.Emergence).Match(periods =>
            { text.AppendLine(Period("Цветение", periods.Flowering)); text.AppendLine(Period("Плодоношение", periods.Fruiting)); return 0; }, error => { text.AppendLine(error); return 0; });
            return 0;
        }, () => 0), () => 0);
        var schedule = document.Preview.Agronomy;
        text.AppendLine("Возможный посев: " + Window(schedule.Sowing));
        text.AppendLine(schedule.ResilientSowing.Match(value => "Устойчивое окно: " + Window(value),
            () => document.Batch.Method is GrowingMethod.Transplant ? "Устойчивого окна нет при заданной неопределённости" : "Прямой посев: период задан вручную"));
        text.AppendLine((document.Batch.Method is GrowingMethod.Transplant ? "Ориентир посева по средним: " : "Ориентир всходов по среднему: ") + Window(schedule.MeanReference));
        text.AppendLine("Прогноз всходов: " + Window(schedule.Emergence));
        Add("Пикировка", schedule.PrickingOut);
        Add("Начало закаливания", schedule.HardeningStart);
        Add("Высадка", schedule.Transplant);
        Add("Прогноз зрелости", schedule.Maturity);
        text.AppendLine("Прорастание: " + Source(document.Batch.Profile.Germination.Source));
        document.Batch.Profile.SeedlingAge.Match(value => { text.AppendLine("Возраст от всходов: " + Source(value.Source)); return 0; }, () => 0);
        document.Quantity.Match(value => { text.AppendLine($"Растения: {document.Batch.TargetPlants.Value}; семена: {value.Seeds.Value} ({(value.IsManual ? "вручную" : "расчёт")})");
            value.ExpectedPlants.Match(count => { text.AppendLine($"Ожидается растений по процентам: {count:0.##}" + (value.HasDeficit ? " — возможен дефицит" : "")); return 0; }, () => 0); return 0; }, () => 0);
        foreach (var diagnostic in schedule.Diagnostics) text.AppendLine(diagnostic.Message);
        text.AppendLine(document.Preview.Lunar switch
        {
            SowingRecommendations.Available available => available.ResilientPreferenceMissed ? "Луна: совпадение только вне устойчивого окна" : "Луна: подходящие дни ниже",
            SowingRecommendations.Conflict conflict => $"Лунный конфликт: в исходном окне нет подходящих дней. Поиск сдвига: ±{conflict.SearchLimitDays} дн.",
            SowingRecommendations.Unavailable unavailable => "Лунная рекомендация недоступна: " + unavailable.Message,
            _ => ""
        });
        var manual = document.Preview.Lunar switch
        {
            SowingRecommendations.Available available => available.ManualChoice,
            SowingRecommendations.Conflict conflict => conflict.ManualChoice,
            _ => Option.None<ManualSowingAssessment>()
        };
        manual.Match(value => { text.AppendLine((document.Batch.Choices.SowingOrigin == PlannedDateOrigin.LunarRecommendation ? "Посев выбран из лунных рекомендаций: " : "Ручной посев: ") + PlanningInputParser.Format(value.Date) + ": "
            + (value.Trend == LunarTrend.Growing ? "растущая Луна" : "убывающая Луна")
            + (value.MatchesPreference ? ", соответствует предпочтению" : ", не соответствует предпочтению")); return 0; }, () => 0);
        return text.ToString();
        void Add(string label, Option<DateWindow> window) => window.Match(value => { text.AppendLine(label + ": " + Window(value)); return 0; }, () => 0);
    }

    public static string Harvest(PlanningBatchDocument document) => HarvestProjection.Calculate(document.Settings.Harvest, document.Batch.TargetPlants.Value).Match(value =>
    {
        var text = new StringBuilder();
        value.Capacity.Match(bed => { text.AppendLine($"Вместимость: {bed.Rows} ряд. × {bed.PerRow} = {bed.Plants}; площадь {bed.AreaM2:0.##} м². Совместимость с другими партиями на грядке не проверена."); return 0; }, () => 0);
        if (document.Settings.Harvest.PerPlantKg is Option<decimal>.Some plant)
            text.AppendLine($"Норма {plant.Value} кг/растение ({Source(document.Settings.Harvest.PerPlantSource)}); прогноз {value.PerPlantKg.Match(x => x.ToString("0.##"), () => "неизвестно")} кг");
        if (document.Settings.Harvest.PerM2Kg is Option<decimal>.Some area)
            text.AppendLine($"Норма {area.Value} кг/м² ({Source(document.Settings.Harvest.PerM2Source)}); прогноз по выделенной площади {value.PerM2Kg.Match(x => x.ToString("0.##") + " кг", () => "неизвестен — задайте площадь")}.");
        document.Settings.Harvest.TargetKg.Match(target => { text.AppendLine($"Цель {target} кг; требуется {value.TargetPlants} растений."); return 0; }, () => 0);
        value.PlantDeficit.Match(deficit => { text.AppendLine($"Нехватка мест: {deficit}"); return 0; }, () => 0);
        value.RequiredAreaM2.Match(required => { text.AppendLine($"Требуется {required:0.###} м²; нехватка площади: {value.AreaDeficitM2.Match(x => x.ToString("0.###"), () => "неизвестно")} м²."); return 0; }, () => 0);
        if (value.UsesDensityEstimate) text.AppendLine("Число растений для нормы на м² — оценка по площади питания, не отдельная агрономическая норма.");
        return text.ToString();
    }, error => error);

    public static string SeasonHarvest(IEnumerable<StoredPlannedBatchFromViewModel> batches)
    {
        try
        {
            var forecasts = batches.DistinctBy(x => x.Id).Select(x => HarvestProjection.Calculate(x.Document.Settings.Harvest, x.Document.Batch.TargetPlants.Value))
                .Where(x => x.IsSuccess).Select(x => x.ValueOrThrow()).ToArray();
            return $"Сезон, прогнозы отдельно: кг/растение — {forecasts.Sum(x => x.PerPlantKg.Match(v => v, () => 0m)):0.##} кг (неизвестно: {forecasts.Count(x => x.PerPlantKg is Option<decimal>.None)}); "
                + $"кг/м² — {forecasts.Sum(x => x.PerM2Kg.Match(v => v, () => 0m)):0.##} кг (неизвестно: {forecasts.Count(x => x.PerM2Kg is Option<decimal>.None)}).";
        }
        catch (OverflowException) { return "Сезонный прогноз превышает допустимый диапазон"; }
    }

    public static IEnumerable<SowingCandidate> Candidates(SowingRecommendations result) => result switch
    {
        SowingRecommendations.Available available => available.Candidates,
        SowingRecommendations.Conflict conflict => Alternatives(conflict),
        _ => []
    };
    private static IEnumerable<SowingCandidate> Alternatives(SowingRecommendations.Conflict conflict)
    {
        if (conflict.Earlier is Option<SowingCandidate>.Some earlier) yield return earlier.Value;
        if (conflict.Later is Option<SowingCandidate>.Some later) yield return later.Value;
    }
}
