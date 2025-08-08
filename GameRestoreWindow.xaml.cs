using System.Windows;
using Connect4Client.Models;
using Connect4Client.Services;
using Connect4Client.Data;

namespace Connect4Client
{
    public partial class GameRestoreWindow : Window
    {
        private readonly GameService gameService;
        private readonly int playerId;
        public SavedGame? SelectedGame { get; private set; }

        public GameRestoreWindow(int playerId)
        {
            InitializeComponent();
            this.playerId = playerId;
            gameService = new GameService();
            LoadSavedGames();
        }

        private async void LoadSavedGames()
        {
            try
            {
                var savedGames = await gameService.GetSavedGamesForPlayer(playerId);
                
                if (savedGames == null || savedGames.Count == 0)
                {
                    SavedGamesGrid.Visibility = Visibility.Collapsed;
                    NoGamesPanel.Visibility = Visibility.Visible;
                    btnRestore.IsEnabled = false;
                }
                else
                {
                    SavedGamesGrid.ItemsSource = savedGames;
                    SavedGamesGrid.Visibility = Visibility.Visible;
                    NoGamesPanel.Visibility = Visibility.Collapsed;
                    btnRestore.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading saved games: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                SavedGamesGrid.Visibility = Visibility.Collapsed;
                NoGamesPanel.Visibility = Visibility.Visible;
                btnRestore.IsEnabled = false;
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (SavedGamesGrid.SelectedItem is SavedGame selectedGame)
            {
                SelectedGame = selectedGame;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a game to restore.", "Selection Required", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 