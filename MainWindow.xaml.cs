using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Connect4Client.Models;
using Connect4Client.Services;
using Connect4Client.Data;

namespace Connect4Client
{
    public partial class MainWindow : Window
    {
        private const int ROWS = 6;
        private const int COLUMNS = 7;

        private Rectangle[,] gameBoard = null!;
        private Ellipse[,] gamePieces = null!;
        private int[,] boardState = null!;
        private bool isPlayerTurn = true;
        private bool gameActive = false;
        private bool isProcessingMove = false;
        private Player? currentPlayer;
        private GameDto? currentGame;

        private List<MoveRecord> turnHistory = new List<MoveRecord>();

        private bool isLoadingGame = false;

        private DispatcherTimer animationTimer = null!;
        private DispatcherTimer cpuMoveTimer = null!;
        private DispatcherTimer colorChangeTimer = null!;
        private Ellipse? droppingPiece;
        private int animationRow = -1;
        private double animationSpeed = 8.0;

        private readonly GameService gameService;
        private readonly ApiService apiService;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                gameService = new GameService();
                apiService = new ApiService();

                SetResponsiveWindowSize();

                InitializeTimers();
                InitializeGameBoard();
                InitializeColumnButtons();
                UpdatePlayerInfo();
                UpdateGameStatus();


            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow initialization error: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Sets the window size based on the user's screen size,
        /// ensuring a responsive and usable layout.
        /// </summary>
        private void SetResponsiveWindowSize()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            Width = Math.Max(800, Math.Min(screenWidth * 0.8, 1200));
            Height = Math.Max(600, Math.Min(screenHeight * 0.8, 900));
        }

        public MainWindow(Player loggedInPlayer) : this()
        {
            SetLoggedInPlayer(loggedInPlayer);
        }

        /// <summary>
        /// Opens the login window. If the player logs in successfully,
        /// the game is initialized with the player's information.
        /// </summary>
        public void ShowLoginDialog()
        {
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && loginWindow.LoggedInPlayer != null)
            {
                SetLoggedInPlayer(loginWindow.LoggedInPlayer);
            }
            else
            {
                MessageBox.Show("You need to login to play games. Click 'Connect to Server' to login later.",
                    "Login Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Sets the currently logged-in player, updates the UI accordingly,
        /// and starts a new game for the player.
        /// </summary>
        private void SetLoggedInPlayer(Player player)
        {
            currentPlayer = player;
            UpdatePlayerInfo();
            UpdateGameStatus();
            ConnectButton.Content = "✓ Connected";
            ConnectButton.IsEnabled = false;

            _ = Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await StartNewGame();
                });
            });
        }

        /// <summary>
        /// Updates the UI with the current player's name and game statistics.
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (currentPlayer != null)
            {
                PlayerNameText.Text = currentPlayer.FirstName;
                UpdateStatistics();
            }
            else
            {
                PlayerNameText.Text = "Not Connected";
            }
        }

        /// <summary>
        /// Initializes all timers used for animations, CPU delays,
        /// and visual effects during the game.
        /// </summary>
        private void InitializeTimers()
        {
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            animationTimer.Tick += AnimationTimer_Tick;

            cpuMoveTimer = new DispatcherTimer();
            cpuMoveTimer.Interval = TimeSpan.FromMilliseconds(1500);
            cpuMoveTimer.Tick += CpuMoveTimer_Tick;

            colorChangeTimer = new DispatcherTimer();
            colorChangeTimer.Interval = TimeSpan.FromMilliseconds(200);
            colorChangeTimer.Tick += ColorChangeTimer_Tick;
        }

        /// <summary>
        /// Initializes the visual game board grid with background rectangles and
        /// hidden ellipse elements for player and CPU pieces.
        /// </summary>
        private void InitializeGameBoard()
        {
            gameBoard = new Rectangle[ROWS, COLUMNS];
            gamePieces = new Ellipse[ROWS, COLUMNS];
            boardState = new int[ROWS, COLUMNS];

            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    Rectangle cell = new Rectangle
                    {
                        Style = (Style)FindResource("GameCellStyle"),
                        Name = $"Cell_{row}_{col}"
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    GameBoardGrid.Children.Add(cell);
                    gameBoard[row, col] = cell;

                    Ellipse piece = new Ellipse
                    {
                        Style = (Style)FindResource("PlayerPieceStyle"),
                        Fill = Brushes.Transparent,
                        Visibility = Visibility.Hidden
                    };

                    Grid.SetRow(piece, row);
                    Grid.SetColumn(piece, col);
                    GameBoardGrid.Children.Add(piece);
                    gamePieces[row, col] = piece;

                    boardState[row, col] = 0;
                }
            }
        }

        /// <summary>
        /// Creates clickable buttons for each column, allowing players
        /// to drop pieces into the selected column.
        /// </summary>
        private void InitializeColumnButtons()
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                Button columnButton = new Button
                {
                    Content = $"▼",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Style = (Style)FindResource("GameButtonStyle"),
                    Tag = col
                };

                columnButton.Click += ColumnButton_Click;
                Grid.SetColumn(columnButton, col);
                ColumnButtonsGrid.Children.Add(columnButton);
            }
        }

        private async void ColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive || !isPlayerTurn || animationTimer.IsEnabled || isProcessingMove)
            {
                if (!gameActive)
                {
                    MessageBox.Show("Please start a new game first by clicking the 'New Game' button.",
                        "Game Not Started", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (isProcessingMove)
                {
                    return;
                }
                return;
            }

            Button clickedButton = (Button)sender;
            int column = (int)clickedButton.Tag;

            await MakePlayerMove(column);
        }

        /// <summary>
        /// Processes the player's move by sending it to the server, updating the board state,
        /// handling CPU responses, animations, and determining game outcome.
        /// </summary>
        private async Task MakePlayerMove(int column)
        {
            if (currentGame == null || currentPlayer == null)
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please login first to make a move.", "Login Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowLoginDialog();
                }
                return;
            }

            if (isProcessingMove)
            {
                return;
            }
            isProcessingMove = true;

            try
            {
                var response = await apiService.MakeMove(currentGame.Id, column);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "Move Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (response.Game == null)
                {
                    return;
                }

                currentGame = response.Game;

                boardState = GameDto.To2DArray(response.Game.Board);

                turnHistory.Add(new MoveRecord(column, true));

                if (response.CpuMove.HasValue)
                {
                    turnHistory.Add(new MoveRecord(response.CpuMove.Value, false));
                }

                await Task.Delay(300);

                await UpdateVisualBoard();

                if (currentGame.Status == "Won")
                {
                    await SaveGameState();
                    await HandleGameWin(true);
                }
                else if (currentGame.Status == "Lost")
                {
                    await Task.Delay(400);

                    await SaveGameState();
                    await HandleGameWin(false);
                }
                else if (currentGame.Status == "Draw")
                {
                    await SaveGameState();
                    await HandleGameDraw();
                }
                else
                {
                    if (response.CpuMove.HasValue)
                    {
                        await Task.Delay(400);
                    }

                    UpdateGameStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error making move: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isProcessingMove = false;
            }
        }

        /// <summary>
        /// Updates the UI to reflect the current board state, including animations
        /// for newly added pieces by the player and CPU.
        /// </summary>
        private async Task UpdateVisualBoard()
        {
            var previousState = new int[ROWS, COLUMNS];
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    var piece = gamePieces[row, col];
                    previousState[row, col] = piece.Visibility == Visibility.Visible ?
                        (piece.Fill == Brushes.Red ? 1 : 2) : 0;
                }
            }

            var newPlayerPieces = new List<(int row, int col)>();
            var newCpuPieces = new List<(int row, int col)>();

            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    var piece = gamePieces[row, col];
                    var newValue = boardState[row, col];
                    var oldValue = previousState[row, col];

                    if (newValue != oldValue && newValue != 0)
                    {
                        if (newValue == 1)
                        {
                            newPlayerPieces.Add((row, col));
                        }
                        else if (newValue == 2)
                        {
                            newCpuPieces.Add((row, col));
                        }
                    }

                    switch (newValue)
                    {
                        case 0:
                            piece.Fill = Brushes.Transparent;
                            piece.Visibility = Visibility.Hidden;
                            break;
                        case 1:
                            if (oldValue == 0)
                            {
                                piece.Fill = Brushes.Red;
                                piece.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                piece.Fill = Brushes.Red;
                                piece.Visibility = Visibility.Visible;
                            }
                            break;
                        case 2:
                            if (oldValue == 0)
                            {
                                piece.Fill = Brushes.Blue;
                                piece.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                piece.Fill = Brushes.Blue;
                                piece.Visibility = Visibility.Visible;
                            }
                            break;
                    }
                }
            }

            foreach (var (row, col) in newPlayerPieces)
            {
                await AnimateSimpleFallingPiece(row, col, true);
                await Task.Delay(100);
            }
            foreach (var (row, col) in newCpuPieces)
            {
                await AnimateSimpleFallingPiece(row, col, false);
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Replays the game from a recorded history of moves by clearing the board
        /// and animating each move sequentially.
        /// </summary>
        private async Task ReplayTurnsFromHistory(List<MoveRecord> turnHistory)
        {
            if (turnHistory == null || turnHistory.Count == 0)
                return;

            // Clear the board completely
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    gamePieces[row, col].Fill = Brushes.Transparent;
                    gamePieces[row, col].Visibility = Visibility.Hidden;
                    boardState[row, col] = 0;
                }
            }

            for (int i = 0; i < turnHistory.Count; i++)
            {
                var move = turnHistory[i];

                int targetRow = -1;
                for (int row = ROWS - 1; row >= 0; row--)
                {
                    if (boardState[row, move.Column] == 0)
                    {
                        targetRow = row;
                        break;
                    }
                }

                if (targetRow != -1)
                {
                    int playerValue = move.IsPlayerMove ? 1 : 2;
                    boardState[targetRow, move.Column] = playerValue;

                    gamePieces[targetRow, move.Column].Fill = move.IsPlayerMove ? Brushes.Red : Brushes.Blue;
                    gamePieces[targetRow, move.Column].Visibility = Visibility.Hidden;

                    await AnimateSimpleFallingPiece(targetRow, move.Column, move.IsPlayerMove);
                    await Task.Delay(100);
                }
            }
        }

        /// <summary>
        /// Simulates a falling piece animation from the top to the target cell.
        /// Replaces the visual with the real game piece and applies a glow effect.
        /// </summary>
        private async Task AnimateSimpleFallingPiece(int targetRow, int targetCol, bool isPlayerMove)
        {
            var piece = gamePieces[targetRow, targetCol];
            var startRow = 0;

            var animatedPiece = new Ellipse
            {
                Width = piece.ActualWidth > 0 ? piece.ActualWidth : 40,
                Height = piece.ActualHeight > 0 ? piece.ActualHeight : 40,
                Fill = isPlayerMove ? Brushes.Red : Brushes.Blue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };

            Grid.SetRow(animatedPiece, startRow);
            Grid.SetColumn(animatedPiece, targetCol);
            GameBoardGrid.Children.Add(animatedPiece);

            for (int currentRow = startRow; currentRow <= targetRow; currentRow++)
            {
                Grid.SetRow(animatedPiece, currentRow);
                await Task.Delay(80);
            }

            GameBoardGrid.Children.Remove(animatedPiece);
            piece.Visibility = Visibility.Visible;

            await AnimateSimpleGlow(piece);
        }

        /// <summary>
        /// Adds a quick glow effect to the given game piece by fading it in and out.
        /// </summary>
        private async Task AnimateSimpleGlow(Ellipse piece)
        {
            var originalOpacity = piece.Opacity;

            piece.Opacity = 0.6;
            await Task.Delay(100);
            piece.Opacity = 1.0;
            await Task.Delay(100);
            piece.Opacity = originalOpacity;
        }

        /// <summary>
        /// Timer tick handler for animating a falling piece inside the Canvas.
        /// Moves the piece down incrementally until it reaches the target row.
        /// </summary>
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (droppingPiece == null) return;

            double currentTop = Canvas.GetTop(droppingPiece);
            double targetTop = animationRow * 60 + 10;

            if (currentTop < targetTop)
            {
                Canvas.SetTop(droppingPiece, currentTop + animationSpeed);

                if (currentTop + animationSpeed >= targetTop - 20)
                {
                    animationSpeed = Math.Max(2.0, animationSpeed * 0.8);
                }
            }
            else
            {
                animationTimer.Stop();
                droppingPiece = null;
                animationSpeed = 8.0;
            }
        }

        private void CpuMoveTimer_Tick(object? sender, EventArgs e)
        {
            cpuMoveTimer.Stop();
            colorChangeTimer.Stop();
        }

        private void AnimateThinking()
        {
            colorChangeTimer.Start();
        }

        private void ColorChangeTimer_Tick(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handles the end of a game in case of win or loss.
        /// Updates game status, player statistics, triggers animations, and saves the result.
        /// </summary>
        private async Task HandleGameWin(bool playerWon)
        {
            gameActive = false;
            colorChangeTimer.Stop();

            if (playerWon)
            {
                GameStatusText.Text = "You Win!";
                GameStatusText.Foreground = Brushes.Green;
                await AnimateWinCelebration();
            }
            else
            {
                GameStatusText.Text = "CPU Wins!";
                GameStatusText.Foreground = Brushes.Red;
            }

            if (currentPlayer != null)
            {
                if (playerWon)
                    currentPlayer.GamesWon++;
                else
                    currentPlayer.GamesLost++;

                currentPlayer.GamesPlayed++;
                UpdateStatistics();

                await apiService.UpdatePlayerStatistics(currentPlayer);
            }

            // Update game status in local database
            if (currentGame != null && currentPlayer != null)
            {
                string finalStatus = playerWon ? "Won" : "Lost";
                await gameService.UpdateGameStatus(currentPlayer.Id, currentGame.Id, finalStatus);

                // Final save of the complete game state
                await SaveGameState();
            }
        }

        /// <summary>
        /// Handles the end of the game when it results in a draw.
        /// Updates player statistics and saves the game state.
        /// </summary>
        private async Task HandleGameDraw()
        {
            gameActive = false;
            colorChangeTimer.Stop();

            GameStatusText.Text = "It's a Draw!";
            GameStatusText.Foreground = Brushes.Blue;

            if (currentPlayer != null)
            {
                currentPlayer.GamesPlayed++;
                UpdateStatistics();
                await apiService.UpdatePlayerStatistics(currentPlayer);
            }

            if (currentGame != null && currentPlayer != null)
            {
                await gameService.UpdateGameStatus(currentPlayer.Id, currentGame.Id, "Draw");

                await SaveGameState();
            }
        }

        /// <summary>
        /// Celebrates a winning state by blinking the winning pieces in green
        /// and alternating back to their original colors.
        /// </summary>
        private async Task AnimateWinCelebration()
        {
            var winningPieces = FindWinningPieces();

            if (winningPieces.Count == 0) return;

            for (int i = 0; i < 5; i++)
            {
                foreach (var (row, col) in winningPieces)
                {
                    gamePieces[row, col].Fill = i % 2 == 0 ? Brushes.LimeGreen :
                        (boardState[row, col] == 1 ? Brushes.Red : Brushes.Blue);
                }

                await Task.Delay(300);
            }
        }

        /// <summary>
        /// Scans the board to find four connected identical pieces indicating a win.
        /// Returns the positions of the winning sequence, or empty list if none found.
        /// </summary>
        private List<(int row, int col)> FindWinningPieces()
        {
            var winningPieces = new List<(int row, int col)>();

            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col <= COLUMNS - 4; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row, col + 1] &&
                        boardState[row, col] == boardState[row, col + 2] &&
                        boardState[row, col] == boardState[row, col + 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row, col + i));
                        }
                        return winningPieces;
                    }
                }
            }

            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col] &&
                        boardState[row, col] == boardState[row + 2, col] &&
                        boardState[row, col] == boardState[row + 3, col])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col));
                        }
                        return winningPieces;
                    }
                }
            }

            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 0; col <= COLUMNS - 4; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col + 1] &&
                        boardState[row, col] == boardState[row + 2, col + 2] &&
                        boardState[row, col] == boardState[row + 3, col + 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col + i));
                        }
                        return winningPieces;
                    }
                }
            }

            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 3; col < COLUMNS; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col - 1] &&
                        boardState[row, col] == boardState[row + 2, col - 2] &&
                        boardState[row, col] == boardState[row + 3, col - 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col - i));
                        }
                        return winningPieces;
                    }
                }
            }

            return winningPieces;
        }

        /// <summary>
        /// Updates the current status of the game (e.g. Not Started, In Progress),
        /// including UI colors based on whose turn it is.
        /// </summary>
        private void UpdateGameStatus()
        {
            if (!gameActive)
            {
                GameStatusText.Text = "Not Started";
                return;
            }

            GameStatusText.Text = "In Progress";
            GameStatusText.Foreground = isPlayerTurn ? Brushes.Green : Brushes.Blue;
        }

        /// <summary>
        /// Displays the player's game statistics in the UI. If no player is connected,
        /// placeholder values are shown instead.
        /// </summary>
        private void UpdateStatistics()
        {
            if (currentPlayer != null)
            {
                GamesWonText.Text = $"Games Won: {currentPlayer.GamesWon}";
                GamesLostText.Text = $"Games Lost: {currentPlayer.GamesLost}";
                GamesPlayedText.Text = $"Games Played: {currentPlayer.GamesPlayed}";
            }
            else
            {
                GamesWonText.Text = "Games Won: --";
                GamesLostText.Text = "Games Lost: --";
                GamesPlayedText.Text = "Games Played: --";
            }
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoadingGame)
            {
                MessageBox.Show("Please wait for the current operation to complete.", "Operation in Progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await StartNewGame();
        }

        private async Task StartNewGame()
        {
            try
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please login first to start a game.", "Login Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowLoginDialog();
                    return;
                }

                isLoadingGame = true;
                LoadGameButton.IsEnabled = false;
                NewGameButton.IsEnabled = false;

                var response = await apiService.StartGame(currentPlayer.PlayerId);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "Game Start Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (response.Game == null)
                {
                    return;
                }

                currentGame = response.Game;

                gameActive = true;
                isPlayerTurn = true;
                isProcessingMove = false;

                turnHistory.Clear();

                boardState = GameDto.To2DArray(response.Game.Board);

                await UpdateVisualBoard();

                animationTimer.Stop();
                cpuMoveTimer.Stop();
                colorChangeTimer.Stop();

                UpdateGameStatus();
                UpdatePlayerInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting new game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isLoadingGame = false;
                LoadGameButton.IsEnabled = true;
                NewGameButton.IsEnabled = true;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginDialog();
        }

        /// <summary>
        /// Saves the final state of the game if it ended (Won/Lost).
        /// Includes board data, player turn, and move history.
        /// </summary>
        private async Task SaveGameState()
        {
            if (currentGame == null || currentPlayer == null)
            {
                return;
            }

            if (currentGame.Status != "Won" && currentGame.Status != "Lost")
            {
                return;
            }

            try
            {
                await gameService.SaveGameState(currentPlayer.Id, boardState, isPlayerTurn, currentGame.Id, currentGame.Status, turnHistory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game state: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoadingGame)
            {
                MessageBox.Show("Please wait for the current operation to complete.", "Operation in Progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please connect to server first!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                isLoadingGame = true;
                LoadGameButton.IsEnabled = false;
                NewGameButton.IsEnabled = false;

                var restoreWindow = new GameRestoreWindow(currentPlayer.Id);
                restoreWindow.Owner = this;
                restoreWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (restoreWindow.ShowDialog() == true && restoreWindow.SelectedGame != null)
                {
                    await RestoreGame(restoreWindow.SelectedGame);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isLoadingGame = false;
                LoadGameButton.IsEnabled = true;
                NewGameButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Restores a saved game from either move history or raw board state.
        /// Reconstructs the game board and updates all necessary UI components.
        /// </summary>
        private async Task RestoreGame(SavedGame savedGame)
        {
            try
            {
                if (savedGame == null)
                {
                    MessageBox.Show("Invalid game data selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<MoveRecord>? turnHistory = null;
                if (!string.IsNullOrEmpty(savedGame.MoveHistoryJson))
                {
                    try
                    {
                        turnHistory = System.Text.Json.JsonSerializer.Deserialize<List<MoveRecord>>(savedGame.MoveHistoryJson);
                    }
                    catch
                    {
                    }
                }

                isPlayerTurn = savedGame.IsPlayerTurn;
                gameActive = true;

                currentGame = new GameDto
                {
                    Id = savedGame.GameId,
                    Status = savedGame.GameStatus,
                    Board = GameDto.From2DArray(boardState),
                    CurrentPlayer = savedGame.IsPlayerTurn ? "Player" : "CPU"
                };

                if (turnHistory != null && turnHistory.Count > 0)
                {
                    await ReplayTurnsFromHistory(turnHistory);
                }
                else
                {
                    boardState = ConvertStringToBoardState(savedGame.BoardStateJson);
                    await UpdateVisualBoard();
                }

                UpdateGameStatus();
                MessageBox.Show("Game restored successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restoring game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                gameActive = false;
                isPlayerTurn = true;
                UpdateGameStatus();
            }
        }

        /// <summary>
        /// Converts a semicolon-separated string into a 6x7 integer board array.
        /// Used to restore the game state from storage.
        /// </summary>
        private static int[,] ConvertStringToBoardState(string boardStateString)
        {
            try
            {
                if (string.IsNullOrEmpty(boardStateString))
                    return new int[6, 7];

                var rows = boardStateString.Split(';');
                if (rows.Length == 0)
                    return new int[6, 7];

                int rowCount = Math.Min(rows.Length, 6);
                int colCount = Math.Min(rows[0].Split(',').Length, 7);
                var result = new int[6, 7];

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
            catch
            {
                return new int[6, 7];
            }
        }
    }
}
