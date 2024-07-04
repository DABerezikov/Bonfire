using BonfireDB.Entities.Base;

namespace BonfireDB.Entities
{
    public class Producer: NamedEntity
    {
        public ICollection<PlantSort> PlantSorts { get; set; }
    }
}
