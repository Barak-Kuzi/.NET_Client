using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connect4Client.Migrations
{
    /// <inheritdoc />
    public partial class AddMoveHistoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoveHistoryJson",
                table: "SavedGames",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SavedGames_PlayerId_GameId",
                table: "SavedGames",
                columns: new[] { "PlayerId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedGames_PlayerId_GameId",
                table: "SavedGames");

            migrationBuilder.DropColumn(
                name: "MoveHistoryJson",
                table: "SavedGames");
        }
    }
}
