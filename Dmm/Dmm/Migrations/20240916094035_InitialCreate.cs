using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dmm.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entry",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bet = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entry", x => x.EntryId);
                });

            migrationBuilder.CreateTable(
                name: "GiveAndTake",
                columns: table => new
                {
                    GiveAndTakeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GtValue = table.Column<int>(type: "int", nullable: false),
                    PmValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiveAndTake", x => x.GiveAndTakeId);
                });

            migrationBuilder.CreateTable(
                name: "NoFightRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestingEntryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvoidEntryName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoFightRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Title",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Title", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "EntryData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WingBan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryData_Entry_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entry",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryId1 = table.Column<int>(type: "int", nullable: false),
                    EntryName1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntryId2 = table.Column<int>(type: "int", nullable: false),
                    EntryName2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TokenId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualMatches_Token_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Token",
                        principalColumn: "TokenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeronEntryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeronOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeronWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MeronWingBan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WalaEntryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WalaOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WalaWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WalaWingBan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Token_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Token",
                        principalColumn: "TokenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "GiveAndTake",
                columns: new[] { "GiveAndTakeId", "EventId", "GtValue", "PmValue" },
                values: new object[] { 1, "TestEventId", 3, 25 });

            migrationBuilder.InsertData(
                table: "Title",
                columns: new[] { "Id", "TitleName" },
                values: new object[] { 1, "ADD EVENT TITLE" });

            migrationBuilder.CreateIndex(
                name: "IX_EntryData_EntryId",
                table: "EntryData",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualMatches_TokenId",
                table: "ManualMatches",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TokenId",
                table: "Matches",
                column: "TokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryData");

            migrationBuilder.DropTable(
                name: "GiveAndTake");

            migrationBuilder.DropTable(
                name: "ManualMatches");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "NoFightRequests");

            migrationBuilder.DropTable(
                name: "Title");

            migrationBuilder.DropTable(
                name: "Entry");

            migrationBuilder.DropTable(
                name: "Token");
        }
    }
}
