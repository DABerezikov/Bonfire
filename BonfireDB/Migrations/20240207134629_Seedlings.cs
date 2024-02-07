using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class Seedlings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedlingInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeedlingNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    MotherPlantId = table.Column<string>(type: "TEXT", nullable: false),
                    LandingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LunarPhase = table.Column<string>(type: "TEXT", nullable: false),
                    PlantPlace = table.Column<string>(type: "TEXT", nullable: false),
                    GerminationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuarantineStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuarantineStopDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuarantineCause = table.Column<string>(type: "TEXT", nullable: false),
                    QuarantineNote = table.Column<string>(type: "TEXT", nullable: false),
                    SeedlingSource = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    QuenchingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeathNote = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedlingInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Replants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplantingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PotVolume = table.Column<double>(type: "REAL", nullable: false),
                    ReplantingNote = table.Column<string>(type: "TEXT", nullable: false),
                    SeedlingInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Replants_SeedlingInfos_SeedlingInfoId",
                        column: x => x.SeedlingInfoId,
                        principalTable: "SeedlingInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seedlings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedlingInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seedlings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seedlings_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seedlings_SeedlingInfos_SeedlingInfoId",
                        column: x => x.SeedlingInfoId,
                        principalTable: "SeedlingInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Treatments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TreatmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Product = table.Column<string>(type: "TEXT", nullable: false),
                    TreatmentMethod = table.Column<string>(type: "TEXT", nullable: false),
                    SeedlingInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Treatments_SeedlingInfos_SeedlingInfoId",
                        column: x => x.SeedlingInfoId,
                        principalTable: "SeedlingInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Replants_SeedlingInfoId",
                table: "Replants",
                column: "SeedlingInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Seedlings_PlantId",
                table: "Seedlings",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Seedlings_SeedlingInfoId",
                table: "Seedlings",
                column: "SeedlingInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_SeedlingInfoId",
                table: "Treatments",
                column: "SeedlingInfoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Replants");

            migrationBuilder.DropTable(
                name: "Seedlings");

            migrationBuilder.DropTable(
                name: "Treatments");

            migrationBuilder.DropTable(
                name: "SeedlingInfos");
        }
    }
}
