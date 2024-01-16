using System.Collections.Generic;

namespace Bonfire.Data;

public static class PlantClassList
{
    private static readonly List<string> ClassList = new() { "Овощи", "Фрукты", "Ягоды", "Зелень", "Цветы" };

    public static IReadOnlyList<string> GetClassList() => ClassList;

}