using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class Update_Seedling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seedlings_SeedlingInfos_SeedlingInfoId",
                table: "Seedlings");

            migrationBuilder.DropIndex(
                name: "IX_Seedlings_SeedlingInfoId",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "SeedlingInfoId",
                table: "Seedlings");

            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                table: "Seedlings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Wight",
                table: "Seedlings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SeedlingId",
                table: "SeedlingInfos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeedlingInfos_SeedlingId",
                table: "SeedlingInfos",
                column: "SeedlingId");

            migrationBuilder.AddForeignKey(
                name: "FK_SeedlingInfos_Seedlings_SeedlingId",
                table: "SeedlingInfos",
                column: "SeedlingId",
                principalTable: "Seedlings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeedlingInfos_Seedlings_SeedlingId",
                table: "SeedlingInfos");

            migrationBuilder.DropIndex(
                name: "IX_SeedlingInfos_SeedlingId",
                table: "SeedlingInfos");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "Wight",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "SeedlingId",
                table: "SeedlingInfos");

            migrationBuilder.AddColumn<int>(
                name: "SeedlingInfoId",
                table: "Seedlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Seedlings_SeedlingInfoId",
                table: "Seedlings",
                column: "SeedlingInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seedlings_SeedlingInfos_SeedlingInfoId",
                table: "Seedlings",
                column: "SeedlingInfoId",
                principalTable: "SeedlingInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
