using System;
using System.Linq;
using BonfireDB.Entities;

namespace Bonfire.Services;

// Сколько рассады доступно к высадке в грядку.
// Из рассады сажают ВЗОШЕДШИЕ живые ростки и считают их ШТУЧНО — независимо от того,
// была ли рассада посеяна по весу (граммы) или по штукам. Вес рассады на доступность
// к высадке не влияет; Quantity (посеяно) — тоже.
internal static class SeedlingAvailability
{
    public static int GerminatedAlive(Seedling seedling) =>
        seedling.SeedlingInfos.Count(i => i.GerminationDate.HasValue && i.IsDead != true);

    public static int Available(Seedling seedling) =>
        Math.Max(0, GerminatedAlive(seedling) - seedling.PlantedOut);
}
