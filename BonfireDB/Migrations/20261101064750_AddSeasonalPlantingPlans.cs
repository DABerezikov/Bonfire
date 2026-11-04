using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonalPlantingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeasonalPlantingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: true),
                    SnapshotRegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegionName = table.Column<string>(type: "TEXT", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalPlantingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonalPlantingPlans_PlanningRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "PlanningRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlannedBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureId = table.Column<int>(type: "INTEGER", nullable: true),
                    SortId = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<int>(type: "INTEGER", nullable: false),
                    Placement = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<string>(type: "TEXT", nullable: false),
                    SelectedSowingDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedBatches_PlantsCulture_CultureId",
                        column: x => x.CultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlannedBatches_PlantsSort_SortId",
                        column: x => x.SortId,
                        principalTable: "PlantsSort",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlannedBatches_SeasonalPlantingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SeasonalPlantingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedBatches_CultureId_SortId_Location_Placement",
                table: "PlannedBatches",
                columns: new[] { "CultureId", "SortId", "Location", "Placement" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedBatches_PlanId_Ordinal",
                table: "PlannedBatches",
                columns: new[] { "PlanId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedBatches_SelectedSowingDate",
                table: "PlannedBatches",
                column: "SelectedSowingDate");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedBatches_SortId",
                table: "PlannedBatches",
                column: "SortId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalPlantingPlans_RegionId",
                table: "SeasonalPlantingPlans",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalPlantingPlans_SeasonYear",
                table: "SeasonalPlantingPlans",
                column: "SeasonYear");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedBatches");

            migrationBuilder.DropTable(
                name: "SeasonalPlantingPlans");
        }
    }
}
