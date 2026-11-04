using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddHarvestJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HarvestJournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MassKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: false),
                    RegionName = table.Column<string>(type: "TEXT", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: false),
                    SnapshotCultureId = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotSortId = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureName = table.Column<string>(type: "TEXT", nullable: false),
                    SortName = table.Column<string>(type: "TEXT", nullable: false),
                    SnapshotBatchId = table.Column<int>(type: "INTEGER", nullable: true),
                    SnapshotPerennialId = table.Column<int>(type: "INTEGER", nullable: true),
                    CultureId = table.Column<int>(type: "INTEGER", nullable: true),
                    SortId = table.Column<int>(type: "INTEGER", nullable: true),
                    BatchId = table.Column<int>(type: "INTEGER", nullable: true),
                    PerennialId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarvestJournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HarvestJournalEntries_PlannedBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PlannedBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HarvestJournalEntries_PlantsCulture_CultureId",
                        column: x => x.CultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HarvestJournalEntries_PlantsSort_SortId",
                        column: x => x.SortId,
                        principalTable: "PlantsSort",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_BatchId",
                table: "HarvestJournalEntries",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_CultureId",
                table: "HarvestJournalEntries",
                column: "CultureId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_SeasonYear_SnapshotCultureId_SnapshotSortId",
                table: "HarvestJournalEntries",
                columns: new[] { "SeasonYear", "SnapshotCultureId", "SnapshotSortId" });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_SnapshotBatchId",
                table: "HarvestJournalEntries",
                column: "SnapshotBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_SortId",
                table: "HarvestJournalEntries",
                column: "SortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HarvestJournalEntries");
        }
    }
}
