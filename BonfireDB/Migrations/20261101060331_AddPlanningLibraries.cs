using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningLibraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningCultureProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CultureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Germination_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Germination_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    SeedlingAge_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    SeedlingAge_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Vegetation_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Vegetation_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_PrickingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras_PrickingOut_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_PrickingOut_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_HardeningMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras_Hardening_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_Hardening_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_GerminationPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Extras_LossPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Extras_LunarGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_ReferenceSoilTemperatureC = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningCultureProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningCultureProfiles_PlantsCulture_CultureId",
                        column: x => x.CultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningRegions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StableKey = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: false),
                    LastSpringFrost_StartMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSpringFrost_StartDay = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSpringFrost_EndMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSpringFrost_EndDay = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSpringFrost_EndInNextYear = table.Column<bool>(type: "INTEGER", nullable: true),
                    FirstAutumnFrost_StartMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstAutumnFrost_StartDay = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstAutumnFrost_EndMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstAutumnFrost_EndDay = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstAutumnFrost_EndInNextYear = table.Column<bool>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningRegions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSortSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SortId = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras_PrickingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras_PrickingOut_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_PrickingOut_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_HardeningMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras_Hardening_Min = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_Hardening_Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_GerminationPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Extras_LossPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Extras_LunarGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    Extras_ReferenceSoilTemperatureC = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSortSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningSortSettings_PlantsSort_SortId",
                        column: x => x.SortId,
                        principalTable: "PlantsSort",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningRegionalWindows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<int>(type: "INTEGER", nullable: false),
                    Placement = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<int>(type: "INTEGER", nullable: false),
                    Window_StartMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Window_StartDay = table.Column<int>(type: "INTEGER", nullable: false),
                    Window_EndMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    Window_EndDay = table.Column<int>(type: "INTEGER", nullable: false),
                    Window_EndInNextYear = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningRegionalWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningRegionalWindows_PlanningRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "PlanningRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningRegionalWindows_PlantsCulture_CultureId",
                        column: x => x.CultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PlanningRegions",
                columns: new[] { "Id", "IsArchived", "Name", "Note", "Source", "StableKey", "TimeZoneId" },
                values: new object[] { 1, false, "Томск", null, null, "tomsk", "Asia/Tomsk" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCultureProfiles_CultureId",
                table: "PlanningCultureProfiles",
                column: "CultureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRegionalWindows_CultureId",
                table: "PlanningRegionalWindows",
                column: "CultureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRegionalWindows_RegionId_CultureId_Location_Placement_Purpose",
                table: "PlanningRegionalWindows",
                columns: new[] { "RegionId", "CultureId", "Location", "Placement", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRegions_StableKey",
                table: "PlanningRegions",
                column: "StableKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSortSettings_SortId",
                table: "PlanningSortSettings",
                column: "SortId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanningCultureProfiles");

            migrationBuilder.DropTable(
                name: "PlanningRegionalWindows");

            migrationBuilder.DropTable(
                name: "PlanningSortSettings");

            migrationBuilder.DropTable(
                name: "PlanningRegions");
        }
    }
}
