using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonfireDB.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantsCulture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Class = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantsCulture", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Producers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeedsInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeightPack = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityPack = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CostPack = table.Column<decimal>(type: "TEXT", nullable: false),
                    DisposeComment = table.Column<string>(type: "TEXT", nullable: true),
                    AmountSeeds = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountSeedsWeight = table.Column<int>(type: "INTEGER", nullable: true),
                    SeedSource = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantsSort",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProducerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinGerminationTime = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxGerminationTime = table.Column<int>(type: "INTEGER", nullable: true),
                    AgeOfSeedlings = table.Column<int>(type: "INTEGER", nullable: true),
                    GrowingSeason = table.Column<int>(type: "INTEGER", nullable: true),
                    LandingPattern = table.Column<int>(type: "INTEGER", nullable: true),
                    PlantHeight = table.Column<int>(type: "INTEGER", nullable: true),
                    PlantColor = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantsSort", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantsSort_Producers_ProducerId",
                        column: x => x.ProducerId,
                        principalTable: "Producers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantCultureId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantSortId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_PlantsCulture_PlantCultureId",
                        column: x => x.PlantCultureId,
                        principalTable: "PlantsCulture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plants_PlantsSort_PlantSortId",
                        column: x => x.PlantSortId,
                        principalTable: "PlantsSort",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedsInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seeds_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seeds_SeedsInfo_SeedsInfoId",
                        column: x => x.SeedsInfoId,
                        principalTable: "SeedsInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantCultureId",
                table: "Plants",
                column: "PlantCultureId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_PlantSortId",
                table: "Plants",
                column: "PlantSortId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantsSort_ProducerId",
                table: "PlantsSort",
                column: "ProducerId");

            migrationBuilder.CreateIndex(
                name: "IX_Seeds_PlantId",
                table: "Seeds",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Seeds_SeedsInfoId",
                table: "Seeds",
                column: "SeedsInfoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Seeds");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "SeedsInfo");

            migrationBuilder.DropTable(
                name: "PlantsCulture");

            migrationBuilder.DropTable(
                name: "PlantsSort");

            migrationBuilder.DropTable(
                name: "Producers");
        }
    }
}
