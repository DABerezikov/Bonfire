using BonfireDB.Entities;
using System.Collections.Generic;
using System.Windows;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models
{
    public class SeedBedFromViewModel : ViewModel
    {

        private Point _Position;
        public Point Position
        {
            get => _Position;
            set => Set(ref _Position, value);
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get => _IsSelected;
            set => Set(ref _IsSelected, value);
        }

        private double _Width;
        public double Width
        {
            get => _Width;
            set => Set(ref _Width, value);
        }
        
        private double _Height;
        public double Height
        {
            get => _Height;
            set => Set(ref _Height, value);
        }
        
       
        public bool IsRegular { get; set; }
        public bool IsHotBed { get; set; }
        public bool IsAutoWatering { get; set; }

        public Soil Soil { get; set; }
        public List<Planting> Plantings { get; set; }

    }
}
