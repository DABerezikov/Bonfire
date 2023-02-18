using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonfireDB.Entities.Base;

namespace BonfireDB.Entities
{
    public class Producer: NamedEntity
    {
        public ICollection<PlantSort> PlantSorts { get; set; }
    }
}
