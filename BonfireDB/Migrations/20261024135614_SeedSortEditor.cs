using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class SeedSortEditor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LandingRowSpacing",
                table: "PlantsSort",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Maturity",
                table: "PlantsSort",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAgeOfSeedlings",
                table: "PlantsSort",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxFruitWeight",
                table: "PlantsSort",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlantHeight",
                table: "PlantsSort",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxGrowingSeason",
                table: "PlantsSort",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinFruitWeight",
                table: "PlantsSort",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "YieldKg",
                table: "PlantsSort",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SortOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SortOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SortOptions_Category_Name",
                table: "SortOptions",
                columns: new[] { "Category", "Name" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO SortOptions (Category, Name)
                SELECT DISTINCT 'Цвет', trim(PlantColor) FROM PlantsSort
                WHERE PlantColor IS NOT NULL AND trim(PlantColor) <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SortOptions");

            migrationBuilder.DropColumn(
                name: "LandingRowSpacing",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "Maturity",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "MaxAgeOfSeedlings",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "MaxFruitWeight",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "MaxPlantHeight",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "MaxGrowingSeason",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "MinFruitWeight",
                table: "PlantsSort");

            migrationBuilder.DropColumn(
                name: "YieldKg",
                table: "PlantsSort");
        }
    }
}
