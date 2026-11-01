using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;
namespace BonfireDB.Harvest;
internal static class HarvestJournalConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var entry = model.Entity<HarvestEntryRow>();
        entry.ToTable("HarvestJournalEntries");
        entry.Property(x => x.Version).IsConcurrencyToken();
        entry.HasIndex(x => new { x.SeasonYear, x.SnapshotCultureId, x.SnapshotSortId });
        entry.HasIndex(x => x.SnapshotBatchId);
        entry.HasOne<PlantCulture>().WithMany().HasForeignKey(x => x.CultureId).OnDelete(DeleteBehavior.SetNull);
        entry.HasOne<PlantSort>().WithMany().HasForeignKey(x => x.SortId).OnDelete(DeleteBehavior.SetNull);
        entry.HasOne<PlannedBatchRow>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.SetNull);
    }
}
