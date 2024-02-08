using System;

namespace Bonfire.Models
{
    public class SeedlingsFromViewModel
    {
        internal int Id { get; set; }
        public string? Culture { get; set; }
        public string? Sort { get; set; }
        public string? Producer { get; set; }
        public int Amount { get; set; }
        public DateTime LandingData { get; set; }
        public DateTime GerminationData { get; set; }
        public DateTime QuenchingDate { get; set; }
        public bool IsQuarantine { get; set; }


    }
}
