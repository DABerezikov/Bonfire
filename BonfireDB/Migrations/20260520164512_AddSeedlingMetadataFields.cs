using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedlingMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LandingDate",
                table: "Seedlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LunarPhase",
                table: "Seedlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantPlace",
                table: "Seedlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeedlingSource",
                table: "Seedlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "GardenPlots",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandingDate",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "LunarPhase",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "PlantPlace",
                table: "Seedlings");

            migrationBuilder.DropColumn(
                name: "SeedlingSource",
                table: "Seedlings");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "GardenPlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
