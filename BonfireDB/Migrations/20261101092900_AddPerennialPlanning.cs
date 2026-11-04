using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddPerennialPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Extras_LifeCycleJson",
                table: "PlanningSortSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extras_LifeCycleJson",
                table: "PlanningCultureProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerennialPlantings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    EstablishmentYear = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureId = table.Column<int>(type: "INTEGER", nullable: true),
                    SortId = table.Column<int>(type: "INTEGER", nullable: true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: true),
                    DefinitionJson = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerennialPlantings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerennialPlantings_PlanningRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "PlanningRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PerennialPlantings_PlantsCulture_CultureId",
                        column: x => x.CultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PerennialPlantings_PlantsSort_SortId",
                        column: x => x.SortId,
                        principalTable: "PlantsSort",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlannedPerennialSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantingId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedPerennialSeasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedPerennialSeasons_PerennialPlantings_PlantingId",
                        column: x => x.PlantingId,
                        principalTable: "PerennialPlantings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedPerennialSeasons_SeasonalPlantingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SeasonalPlantingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HarvestJournalEntries_PerennialId",
                table: "HarvestJournalEntries",
                column: "PerennialId");

            migrationBuilder.CreateIndex(
                name: "IX_PerennialPlantings_CultureId",
                table: "PerennialPlantings",
                column: "CultureId");

            migrationBuilder.CreateIndex(
                name: "IX_PerennialPlantings_EstablishmentYear",
                table: "PerennialPlantings",
                column: "EstablishmentYear");

            migrationBuilder.CreateIndex(
                name: "IX_PerennialPlantings_RegionId",
                table: "PerennialPlantings",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_PerennialPlantings_SortId",
                table: "PerennialPlantings",
                column: "SortId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedPerennialSeasons_PlanId_PlantingId",
                table: "PlannedPerennialSeasons",
                columns: new[] { "PlanId", "PlantingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedPerennialSeasons_PlantingId",
                table: "PlannedPerennialSeasons",
                column: "PlantingId");

            migrationBuilder.AddForeignKey(
                name: "FK_HarvestJournalEntries_PerennialPlantings_PerennialId",
                table: "HarvestJournalEntries",
                column: "PerennialId",
                principalTable: "PerennialPlantings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HarvestJournalEntries_PerennialPlantings_PerennialId",
                table: "HarvestJournalEntries");

            migrationBuilder.DropTable(
                name: "PlannedPerennialSeasons");

            migrationBuilder.DropTable(
                name: "PerennialPlantings");

            migrationBuilder.DropIndex(
                name: "IX_HarvestJournalEntries_PerennialId",
                table: "HarvestJournalEntries");

            migrationBuilder.DropColumn(
                name: "Extras_LifeCycleJson",
                table: "PlanningSortSettings");

            migrationBuilder.DropColumn(
                name: "Extras_LifeCycleJson",
                table: "PlanningCultureProfiles");
        }
    }
}
