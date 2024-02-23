﻿using BonfireDB.Entities.Base;

namespace BonfireDB.Entities;

public class Seedling: Entity
{
    public  Plant Plant { get; set; }
    public double Weight { get; set; }
    public double Quantity { get; set; }
    public List<SeedlingInfo> SeedlingInfos { get; set; }
   

}