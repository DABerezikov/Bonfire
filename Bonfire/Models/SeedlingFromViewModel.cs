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
        private double? _Weight;

        public double? Weight
        {
            get => _Weight != 0 ? _Weight : null;
            set => _Weight = value;
            
        }

        private double? _Quantity;
        public double? Quantity
        {
            get => _Quantity != 0 ? _Quantity : null;
            set => _Quantity = value;

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

        public int CountGerminate => SeedlingInfos != null && SeedlingInfos.Count != 0 ? SeedlingInfos.Count : 0;
    }
}
