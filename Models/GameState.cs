namespace Connect4Client.Models
{
    public class GameState
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int[,] BoardState { get; set; } = new int[6, 7];
        public bool IsPlayerTurn { get; set; }
        public DateTime SavedAt { get; set; }
        public string? GameStatus { get; set; }
        public List<MoveRecord>? MoveHistory { get; set; }
    }
    
    public class MoveRecord
    {
        public int Column { get; set; }
        public bool IsPlayerMove { get; set; }
        
        public MoveRecord() { }
        
        public MoveRecord(int column, bool isPlayerMove)
        {
            Column = column;
            IsPlayerMove = isPlayerMove;
        }
    }
} 