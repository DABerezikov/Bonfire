using System.Collections.Generic;
using System.Windows.Documents;

namespace Bonfire.Data;

public static class PlantClassList
{
    private static readonly List<string> _ClassList = new () { "Овощи", "Фрукты", "Ягоды", "Зелень", "Цветы" };

    public static List<string> GetClassList() => _ClassList;

}