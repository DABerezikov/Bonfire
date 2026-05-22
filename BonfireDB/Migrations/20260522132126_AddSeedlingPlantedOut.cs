using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedlingPlantedOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantedOut",
                table: "Seedlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlantedOut",
                table: "Seedlings");
        }
    }
}
