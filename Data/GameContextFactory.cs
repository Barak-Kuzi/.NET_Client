using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace Connect4Client.Data
{
    public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string dbPath = Path.Combine(currentDirectory, "Connect4ClientDb.mdf");
            
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            optionsBuilder.UseSqlServer($"Server=(localdb)\\mssqllocaldb;AttachDbFilename={dbPath};Database=Connect4ClientDb;Trusted_Connection=True;MultipleActiveResultSets=true");

            return new GameContext(optionsBuilder.Options);
        }
    }
} 