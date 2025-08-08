using System.Windows;
using System.Windows.Media;
using Connect4Client.Models;
using Connect4Client.Services;
using System.Diagnostics;

namespace Connect4Client
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService apiService;
        public Player? LoggedInPlayer { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            apiService = new ApiService();
            txtPlayerId.Focus();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;
            
            if (string.IsNullOrWhiteSpace(txtPlayerId.Text))
            {
                ShowError("Please enter your Player ID");
                return;
            }

            if (!int.TryParse(txtPlayerId.Text, out int playerId))
            {
                ShowError("Player ID must be a number");
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "Checking...";

            try
            {
                var connectionTest = await apiService.TestConnection();
                if (!connectionTest)
                {
                    ShowError("Cannot connect to server. Please make sure the server is running.");
                    return;
                }

                var player = await apiService.GetPlayerByPlayerId(playerId);
                if (player == null)
                {
                    ShowError("Player not found. Please register first on the website.");
                    return;
                }

                LoggedInPlayer = player;
                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Login";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5000/Register",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError($"Cannot open browser: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
} 