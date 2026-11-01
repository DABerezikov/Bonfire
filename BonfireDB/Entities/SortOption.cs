using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

/// <summary>Сохранённые пользовательские варианты цвета и созревания.</summary>
public class SortOption : NamedEntity
{
    public string Category { get; set; } = string.Empty;
}