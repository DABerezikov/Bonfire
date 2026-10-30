using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Models;

/// <summary>Особые значения производителя.</summary>
public static class Producers
{
    /// <summary>Псевдо-производитель для собственных (собранных) семян.</summary>
    public const string Own = "Свои семена";
}

/// <summary>Единицы измерения количества семян/рассады.</summary>
public static class Units
{
    /// <summary>Варианты выпадающего списка при добавлении семян.</summary>
    public const string GramsOption  = "Граммы";
    public const string PiecesOption = "Штуки";

    public static Result<SeedUnit> ResolveSeedUnit(string value) => value switch
    {
        GramsOption => Result.Success(SeedUnit.Grams),
        PiecesOption => Result.Success(SeedUnit.Pieces),
        _ => Result.Failure<SeedUnit>("Выберите единицу количества семян")
    };

    /// <summary>Краткая подпись грамм при добавлении рассады.</summary>
    public const string GramsAbbr = "гр.";

    /// <summary>Краткая подпись грамм в планировщике и отчётах.</summary>
    public const string GramAbbr = "г.";

    /// <summary>Краткая подпись штук.</summary>
    public const string PiecesAbbr = "шт.";
}
