namespace Bonfire.Models;

/// <summary>
/// Источники получения семян и рассады. Значения хранятся в БД
/// (SeedsInfo.SeedSource / Seedling.SeedlingSource) и привязаны к радиокнопкам UI.
/// </summary>
public static class PlantSources
{
    public const string Purchased = "Куплено";
    public const string Donated   = "Подарено";
    public const string FromSeeds = "Из семян";
    public const string Collected = "Собрано";
}

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

    /// <summary>Краткая подпись грамм при добавлении рассады.</summary>
    public const string GramsAbbr = "гр.";

    /// <summary>Краткая подпись грамм в планировщике и отчётах.</summary>
    public const string GramAbbr = "г.";

    /// <summary>Краткая подпись штук.</summary>
    public const string PiecesAbbr = "шт.";
}
