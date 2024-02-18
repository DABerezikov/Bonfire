using System;

namespace Bonfire.Models;

public class PlantFromViewModel
{
    internal int Id { get; set; }
    public string? Culture { get; set; }
    public string? Sort { get; set; }
    public string? Producer { get; set; }
    public DateTime ExpirationDate { get; set; }

    public override string ToString() => Producer + " " + ExpirationDate.Year;
    
}