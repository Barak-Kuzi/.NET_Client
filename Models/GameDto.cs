namespace Connect4Client.Models
{
    public class GameDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int[][] Board { get; set; } = new int[6][];
        public string CurrentPlayer { get; set; } = string.Empty;
        public string? Winner { get; set; }
        public PlayerDto? Player { get; set; }

        public GameDto()
        {
            Board = new int[6][];
            for (int i = 0; i < 6; i++)
            {
                Board[i] = new int[7];
            }
        }

        public static int[][] From2DArray(int[,] array2D)
        {
            int rows = array2D.GetLength(0);
            int cols = array2D.GetLength(1);
            int[][] jaggedArray = new int[rows][];
            
            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                {
                    jaggedArray[i][j] = array2D[i, j];
                }
            }
            return jaggedArray;
        }

        public static int[,] To2DArray(int[][] jaggedArray)
        {
            if (jaggedArray == null || jaggedArray.Length == 0)
                return new int[6, 7];
                
            int rows = jaggedArray.Length;
            int cols = jaggedArray[0].Length;
            int[,] array2D = new int[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array2D[i, j] = jaggedArray[i][j];
                }
            }
            return array2D;
        }
    }

    public class PlayerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
    }

    public class MakeMoveRequest
    {
        public int GameId { get; set; }
        public int Column { get; set; }
    }

    public class MakeMoveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public GameDto? Game { get; set; }
        public int? CpuMove { get; set; }
    }

    public class StartGameRequest
    {
        public int PlayerId { get; set; }
    }

    public class StartGameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public GameDto? Game { get; set; }
    }
} 