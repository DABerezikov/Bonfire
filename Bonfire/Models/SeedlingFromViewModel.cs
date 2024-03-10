using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bonfire.Models
{
    public class SeedlingFromViewModel
    {
        internal int Id { get; set; }
        public string? Culture { get; set; }
        public string? Sort { get; set; }
        public string? Producer { get; set; }
        private double? weight;

        public double? Weight
        {
            get => weight != 0 ? weight : null;
            set => weight = value;
            
        }

        private double? quantity;
        public double? Quantity
        {
            get => quantity != 0 ? quantity : null;
            set => quantity = value;

        }
        public DateTime? LandingData { get; set; }
        public string? SeedlingMoonPhase { get; set; }
        public ObservableCollection<SeedlingInfoFromViewModel>? SeedlingInfos { get; set; }

        public int? MinGerminate
        {
            get
            {
                if (SeedlingInfos!.Count <= 1) return null;
                var minDate = SeedlingInfos.Min(d => d.GerminationData);
                return (minDate -  LandingData)!.Value.Days;
            }
        }

        public int? MaxGerminate
        {
            get
            {
                if (SeedlingInfos!.Count <= 1) return null;
                var maxDate = SeedlingInfos.Max(d => d.GerminationData);
                return (maxDate - LandingData)!.Value.Days;
            }
        }

        public int CountGerminate
        {
            get
            {
                if (SeedlingInfos.Count != 0) return SeedlingInfos.Count;
                return 0;
            }
        }
    }
}
