using Connect4Client.Data;
using Connect4Client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Connect4Client.Services
{
    public class GameService
    {
        private readonly IServiceProvider serviceProvider;

        public GameService()
        {
            serviceProvider = ((App)App.Current).Services ?? throw new InvalidOperationException("Service provider not initialized");
        }

        /// <summary>
        /// Saves or updates a player's game state in the database (only for finished games)
        /// </summary>
        public async Task SaveGameState(int playerId, int[,] boardState, bool isPlayerTurn, int gameId, string gameStatus, List<MoveRecord>? turnHistory = null)
        {
            try
            {
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider is null");
                }

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();

                string boardStateString = ConvertBoardStateToString(boardState);
                string turnHistoryString = turnHistory != null ? System.Text.Json.JsonSerializer.Serialize(turnHistory) : "";

                var existingGame = await context.SavedGames
                    .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);

                if (existingGame != null)
                {
                    existingGame.BoardStateJson = boardStateString;
                    existingGame.IsPlayerTurn = isPlayerTurn;
                    existingGame.SavedAt = DateTime.Now;
                    existingGame.GameStatus = gameStatus;
                    existingGame.MoveHistoryJson = turnHistoryString;
                }
                else
                {
                    var savedGame = new SavedGame
                    {
                        PlayerId = playerId,
                        GameId = gameId,
                        BoardStateJson = boardStateString,
                        IsPlayerTurn = isPlayerTurn,
                        SavedAt = DateTime.Now,
                        GameStatus = gameStatus,
                        MoveHistoryJson = turnHistoryString
                    };

                    context.SavedGames.Add(savedGame);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error saving game state: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Converts a 2D board state array to a string format for database storage
        /// </summary>
        private string ConvertBoardStateToString(int[,] boardState)
        {
            try
            {
                var rows = new List<string>();
                for (int i = 0; i < boardState.GetLength(0); i++)
                {
                    var row = new List<string>();
                    for (int j = 0; j < boardState.GetLength(1); j++)
                    {
                        row.Add(boardState[i, j].ToString());
                    }
                    rows.Add(string.Join(",", row));
                }
                return string.Join(";", rows);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error converting board state to string: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts a string format board state back to a 2D array
        /// </summary>
        private int[,] ConvertStringToBoardState(string boardStateString)
        {
            try
            {
                if (string.IsNullOrEmpty(boardStateString))
                    return new int[6, 7];

                var rows = boardStateString.Split(';');
                if (rows.Length == 0)
                    return new int[6, 7];

                int rowCount = rows.Length;
                int colCount = rows[0].Split(',').Length;
                var result = new int[rowCount, colCount];

                for (int i = 0; i < rowCount; i++)
                {
                    var cols = rows[i].Split(',');
                    for (int j = 0; j < colCount && j < cols.Length; j++)
                    {
                        if (int.TryParse(cols[j], out int value))
                        {
                            result[i, j] = value;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error converting string to board state: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all saved games for a specific player in chronological order
        /// </summary>
        public async Task<List<SavedGame>> GetSavedGamesForPlayer(int playerId)
        {
            try
            {
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider is null");
                }

                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();

                var allGames = await context.SavedGames
                    .Where(g => g.PlayerId == playerId)
                    .OrderBy(g => g.SavedAt)
                    .ToListAsync();

                // Filter out games that are completely empty (no pieces played)
                var gamesWithPieces = new List<SavedGame>();
                foreach (var game in allGames)
                {
                    if (!string.IsNullOrEmpty(game.BoardStateJson))
                    {
                        var boardState = ConvertStringToBoardState(game.BoardStateJson);
                        if (HasPiecesOnBoard(boardState))
                        {
                            gamesWithPieces.Add(game);
                        }
                    }
                }

                return gamesWithPieces;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting saved games: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a board has any pieces on it
        /// </summary>
        private bool HasPiecesOnBoard(int[,] boardState)
        {
            for (int row = 0; row < boardState.GetLength(0); row++)
            {
                for (int col = 0; col < boardState.GetLength(1); col++)
                {
                    if (boardState[row, col] != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the game status for a specific game
        /// </summary>
        public async Task UpdateGameStatus(int playerId, int gameId, string status)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();

                var savedGame = await context.SavedGames
                    .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);

                if (savedGame != null)
                {
                    savedGame.GameStatus = status;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating game status: {ex.Message}", ex);
            }
        }
    }
}