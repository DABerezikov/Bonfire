namespace BonfireDB.Entities.GardenPlanning;

// Цветник (TPH дискриминатор "FlowerBed")
public class FlowerBed : GardenElement
{
    public string? Shape { get; set; }  // Круглый / фигурный / прямоугольный
}
