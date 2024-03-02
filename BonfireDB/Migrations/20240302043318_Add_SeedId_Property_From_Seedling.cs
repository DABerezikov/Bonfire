using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class Add_SeedId_Property_From_Seedling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeedId",
                table: "Seedlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeedId",
                table: "Seedlings");
        }
    }
}
