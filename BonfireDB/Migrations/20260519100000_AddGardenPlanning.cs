using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddGardenPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GardenPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GardenPlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WidthMeters = table.Column<double>(type: "REAL", nullable: false),
                    HeightMeters = table.Column<double>(type: "REAL", nullable: false),
                    CanvasWidth = table.Column<double>(type: "REAL", nullable: false),
                    CanvasHeight = table.Column<double>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PlotType = table.Column<string>(type: "TEXT", nullable: false),
                    // Garden
                    GardenPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    // Greenhouse
                    ParentPlotId = table.Column<int>(type: "INTEGER", nullable: true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    DisplayWidth = table.Column<double>(type: "REAL", nullable: true),
                    DisplayHeight = table.Column<double>(type: "REAL", nullable: true),
                    Rotation = table.Column<double>(type: "REAL", nullable: true),
                    StateTypeName = table.Column<string>(type: "TEXT", nullable: true),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenPlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GardenPlots_GardenPlans_GardenPlanId",
                        column: x => x.GardenPlanId,
                        principalTable: "GardenPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GardenPlots_GardenPlots_ParentPlotId",
                        column: x => x.ParentPlotId,
                        principalTable: "GardenPlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GardenElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    DisplayWidth = table.Column<double>(type: "REAL", nullable: false),
                    DisplayHeight = table.Column<double>(type: "REAL", nullable: false),
                    Rotation = table.Column<double>(type: "REAL", nullable: false),
                    AreaSquareMeters = table.Column<double>(type: "REAL", nullable: false),
                    StateTypeName = table.Column<string>(type: "TEXT", nullable: false),
                    GridRows = table.Column<int>(type: "INTEGER", nullable: false),
                    GridColumns = table.Column<int>(type: "INTEGER", nullable: false),
                    SoilType = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ElementType = table.Column<string>(type: "TEXT", nullable: false),
                    // Bed
                    Orientation = table.Column<string>(type: "TEXT", nullable: true),
                    // ColdFrame
                    CoverMaterial = table.Column<string>(type: "TEXT", nullable: true),
                    // FlowerBed
                    Shape = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GardenElements_GardenPlots_PlotId",
                        column: x => x.PlotId,
                        principalTable: "GardenPlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantingSpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Row = table.Column<int>(type: "INTEGER", nullable: false),
                    Column = table.Column<int>(type: "INTEGER", nullable: false),
                    GardenElementId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedlingInfoId = table.Column<int>(type: "INTEGER", nullable: true),
                    StateTypeName = table.Column<string>(type: "TEXT", nullable: false),
                    PlantedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HarvestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantingSpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantingSpots_GardenElements_GardenElementId",
                        column: x => x.GardenElementId,
                        principalTable: "GardenElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantingSpots_SeedlingInfos_SeedlingInfoId",
                        column: x => x.SeedlingInfoId,
                        principalTable: "SeedlingInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GardenElements_PlotId",
                table: "GardenElements",
                column: "PlotId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenPlots_GardenPlanId",
                table: "GardenPlots",
                column: "GardenPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenPlots_ParentPlotId",
                table: "GardenPlots",
                column: "ParentPlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantingSpots_GardenElementId",
                table: "PlantingSpots",
                column: "GardenElementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantingSpots_SeedlingInfoId",
                table: "PlantingSpots",
                column: "SeedlingInfoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlantingSpots");
            migrationBuilder.DropTable(name: "GardenElements");
            migrationBuilder.DropTable(name: "GardenPlots");
            migrationBuilder.DropTable(name: "GardenPlans");
        }
    }
}
