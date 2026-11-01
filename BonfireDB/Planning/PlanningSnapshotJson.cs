using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace BonfireDB.Planning;

/// <summary>Версионированная граница хранения; фабрики повторно проверяют value objects при чтении.</summary>
internal static class PlanningSnapshotJson
{
    private const int SchemaVersion = 1;
    private sealed record Envelope(int SchemaVersion, PlanningBatchDocument Document);
    private sealed record WindowValues(DateOnly Start, DateOnly End);
    private sealed record RangeValues(int Min, int Max);
    private sealed record ConditionsValues(GrowingLocation Location, PlantPlacement Placement);
    private sealed record AnnualValues(int StartMonth, int StartDay, int EndMonth, int EndDay, bool EndInNextYear);
    private sealed record RegionalValues(RegionId RegionId, CultureId CultureId, GrowingConditions Conditions,
        RegionalWindowPurpose Purpose, AnnualDateWindow Window, Option<string> Source, Option<string> Note);
    private sealed record BatchValues(CultureId CultureId, SortId SortId, GrowingConditions Conditions, GrowingMethod Method,
        EffectiveGrowingProfile Profile, PlantCount TargetPlants, PlanningChoices Choices);
    private sealed record DocumentValues(PlannedBatchDefinition Batch, PlanningBatchSettings Settings, PlanningReferenceSnapshot References,
        SowingPreviewFromViewModel Preview, Option<SeedQuantityCalculation> Quantity, DateTimeOffset CalculatedAt,
        int CalculationVersion, int LunarPolicyVersion);

    private static readonly JsonSerializerOptions Options = CreateOptions();

    internal static string Write(PlanningBatchDocument document) => JsonSerializer.Serialize(new Envelope(SchemaVersion, document), Options);

    internal static Result<PlanningBatchDocument> Read(string json)
    {
        try
        {
            using var parsed = JsonDocument.Parse(json);
            if (!parsed.RootElement.TryGetProperty(nameof(Envelope.SchemaVersion), out var version)
                || !version.TryGetInt32(out var number) || number != SchemaVersion)
                return Result.Failure<PlanningBatchDocument>("Неподдерживаемая версия формата снимка партии");
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Options);
            return envelope?.Document is { } document ? Result.Success(document)
                : Result.Failure<PlanningBatchDocument>("Снимок партии отсутствует");
        }
        catch (JsonException error)
        {
            return Result.Failure<PlanningBatchDocument>($"Ошибка чтения снимка партии: {error.Message}");
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type == typeof(GrowingMethod))
                SetVariants(info, (typeof(GrowingMethod.Transplant), "transplant"), (typeof(GrowingMethod.DirectSow), "direct"));
            else if (info.Type == typeof(OperationOverride<DayRange>))
                SetVariants(info, (typeof(OperationOverride<DayRange>.Inherit), "inherit"),
                    (typeof(OperationOverride<DayRange>.Disabled), "disabled"), (typeof(OperationOverride<DayRange>.Enabled), "enabled"));
            else if (info.Type == typeof(OperationPlan<DayRange>))
                SetVariants(info, (typeof(OperationPlan<DayRange>.Unknown), "unknown"),
                    (typeof(OperationPlan<DayRange>.Disabled), "disabled"), (typeof(OperationPlan<DayRange>.Enabled), "enabled"));
            else if (info.Type == typeof(SowingRecommendations))
                SetVariants(info, (typeof(SowingRecommendations.Available), "available"),
                    (typeof(SowingRecommendations.Conflict), "conflict"), (typeof(SowingRecommendations.Unavailable), "unavailable"));
        });
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true
        };
        options.Converters.Add(new OptionConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new FactoryConverter<CultureId, int>(x => x.Value, CultureId.Create));
        options.Converters.Add(new FactoryConverter<SortId, int>(x => x.Value, SortId.Create));
        options.Converters.Add(new FactoryConverter<RegionId, int>(x => x.Value, RegionId.Create));
        options.Converters.Add(new FactoryConverter<PlantCount, int>(x => x.Value, PlantCount.Create));
        options.Converters.Add(new FactoryConverter<GerminationPercentage, decimal>(x => x.Value, GerminationPercentage.Create));
        options.Converters.Add(new FactoryConverter<LossPercentage, decimal>(x => x.Value, LossPercentage.Create));
        options.Converters.Add(new FactoryConverter<DateWindow, WindowValues>(x => new(x.Start, x.End), x => DateWindow.Create(x.Start, x.End)));
        options.Converters.Add(new FactoryConverter<DayRange, RangeValues>(x => new(x.Min, x.Max), x => DayRange.Create(x.Min, x.Max)));
        options.Converters.Add(new FactoryConverter<GrowingConditions, ConditionsValues>(x => new(x.Location, x.Placement),
            x => GrowingConditions.Create(x.Location, x.Placement)));
        options.Converters.Add(new FactoryConverter<AnnualDateWindow, AnnualValues>(x => new(x.StartMonth, x.StartDay, x.EndMonth, x.EndDay, x.EndInNextYear),
            x => AnnualDateWindow.Create(x.StartMonth, x.StartDay, x.EndMonth, x.EndDay, x.EndInNextYear)));
        options.Converters.Add(new FactoryConverter<RegionalPlantingWindow, RegionalValues>(
            x => new(x.RegionId, x.CultureId, x.Conditions, x.Purpose, x.Window, x.Source, x.Note),
            x => RegionalPlantingWindow.Create(x.RegionId, x.CultureId, x.Conditions, x.Purpose, x.Window, x.Source, x.Note)));
        options.Converters.Add(new FactoryConverter<PlannedBatchDefinition, BatchValues>(
            x => new(x.CultureId, x.SortId, x.Conditions, x.Method, x.Profile, x.TargetPlants, x.Choices),
            x => PlannedBatchDefinition.Create(x.CultureId, x.SortId, x.Conditions, x.Method, x.Profile, x.TargetPlants, x.Choices)));
        options.Converters.Add(new FactoryConverter<PlanningBatchDocument, DocumentValues>(
            x => new(x.Batch, x.Settings, x.References, x.Preview, x.Quantity, x.CalculatedAt, x.CalculationVersion, x.LunarPolicyVersion),
            x => PlanningBatchDocument.Create(x.Batch, x.Settings, x.References, x.Preview, x.Quantity,
                x.CalculatedAt, x.CalculationVersion, x.LunarPolicyVersion)));
        return options;
    }

    private static void SetVariants(JsonTypeInfo info, params (Type Type, string Tag)[] variants)
    {
        info.PolymorphismOptions = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "$kind" };
        foreach (var variant in variants) info.PolymorphismOptions.DerivedTypes.Add(new(variant.Type, variant.Tag));
    }

    private sealed class FactoryConverter<T, TValues>(Func<T, TValues> values, Func<TValues, Result<T>> create) : JsonConverter<T>
    {
        public override bool HandleNull => true;
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) throw new JsonException($"Отсутствует {typeof(T).Name}");
            var input = JsonSerializer.Deserialize<TValues>(ref reader, options);
            if (input is null) throw new JsonException($"Отсутствует {typeof(T).Name}");
            return create(input).Match(value => value, error => throw new JsonException(error));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, values(value), options);
    }

    private sealed class OptionConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(OptionConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }

    private sealed class OptionConverter<T> : JsonConverter<Option<T>>
    {
        public override bool HandleNull => true;
        public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return Option.None<T>();
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            if (value is null) throw new JsonException("Some не может содержать null");
            return Option.Some(value);
        }

        public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
        {
            if (value is Option<T>.Some some) JsonSerializer.Serialize(writer, some.Value, options);
            else if (value is Option<T>.None) writer.WriteNullValue();
            else throw new JsonException("Не задан Option");
        }
    }
}
