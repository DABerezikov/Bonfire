using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningYieldNorms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Extras_YieldPerM2Kg",
                table: "PlanningSortSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Extras_YieldPerPlantKg",
                table: "PlanningSortSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Extras_YieldPerM2Kg",
                table: "PlanningCultureProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Extras_YieldPerPlantKg",
                table: "PlanningCultureProfiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extras_YieldPerM2Kg",
                table: "PlanningSortSettings");

            migrationBuilder.DropColumn(
                name: "Extras_YieldPerPlantKg",
                table: "PlanningSortSettings");

            migrationBuilder.DropColumn(
                name: "Extras_YieldPerM2Kg",
                table: "PlanningCultureProfiles");

            migrationBuilder.DropColumn(
                name: "Extras_YieldPerPlantKg",
                table: "PlanningCultureProfiles");
        }
    }
}
