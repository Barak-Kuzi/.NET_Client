using Microsoft.EntityFrameworkCore;
using Connect4Client.Models;
using System.Text.Json;

namespace Connect4Client.Data
{
    public class GameContext : DbContext
    {
        public DbSet<SavedGame> SavedGames { get; set; } = default!;

        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SavedGame>()
                .Property(e => e.GameId)
                .IsRequired();

            modelBuilder.Entity<SavedGame>()
                .HasIndex(e => new { e.PlayerId, e.GameId })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class SavedGame
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public string BoardStateJson { get; set; } = "";
        public bool IsPlayerTurn { get; set; }
        public DateTime SavedAt { get; set; }
        public string GameStatus { get; set; } = "InProgress";
        public string MoveHistoryJson { get; set; } = "";
        
        public int MovesCount
        {
            get
            {
                if (string.IsNullOrEmpty(MoveHistoryJson))
                    return 0;
                
                try
                {
                    var moveHistory = System.Text.Json.JsonSerializer.Deserialize<List<MoveRecord>>(MoveHistoryJson);
                    return moveHistory?.Count ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
} 