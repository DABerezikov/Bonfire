namespace BonfireDB.Entities.GardenPlanning;

// Грядка (TPH дискриминатор "Bed")
public class Bed : GardenElement
{
    public string? Orientation { get; set; }  // С-Ю / В-З
}
