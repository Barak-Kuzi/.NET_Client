using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connect4Client.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    BoardStateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPlayerTurn = table.Column<bool>(type: "bit", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GameStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedGames", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedGames");
        }
    }
}
