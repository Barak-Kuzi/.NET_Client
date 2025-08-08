using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connect4Client.Migrations
{
    /// <inheritdoc />
    public partial class AddGameIdToSavedGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "SavedGames",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameId",
                table: "SavedGames");
        }
    }
}
