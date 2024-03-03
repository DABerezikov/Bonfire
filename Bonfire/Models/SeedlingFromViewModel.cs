using System;
using System.Collections.ObjectModel;

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
        public ObservableCollection<SeedlingInfoFromViewModel>? SeedlingInfos { get; set; }


    }
}
