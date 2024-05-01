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
        public DateTime? ReplantingData { get; set; }
        public string? SeedlingMoonPhase { get; set; }
        public bool? IsDead { get; set; }
        public ObservableCollection<SeedlingInfoFromViewModel>? SeedlingInfos { get; set; }

        public int? MinGerminate
        {
            get
            {
                if (SeedlingInfos!.Count < 1) return null;
                var minDate = SeedlingInfos.Min(d => d.GerminationData);
                return (minDate -  LandingData)!.Value.Days;
            }
        }

        public int? MaxGerminate
        {
            get
            {
                if (SeedlingInfos!.Count < 1) return null;
                var maxDate = SeedlingInfos.Max(d => d.GerminationData);
                return (maxDate - LandingData)!.Value.Days;
            }
        }

        public int CountGerminate => GetCountGerminate();

        public int? Balance => SeedlingInfos == null ? null : SeedlingInfos.Count - SeedlingInfos.Select(s => s).Skip(1).Count(s => s.IsDead == true);

        private int GetCountGerminate()
        {
            var count = 0;
            if (SeedlingInfos == null && SeedlingInfos.Count == 0) return count;
            foreach (var info in SeedlingInfos)
            {
                if (info.MotherPlantId != null) continue;
                count++;
            }

            return count;
        }
    }
}
