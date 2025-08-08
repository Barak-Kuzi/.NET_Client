using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Connect4Client.Data;
using Connect4Client.Models;
using System.IO;

namespace Connect4Client
{
    public partial class App : Application
    {
        private ServiceProvider? serviceProvider;
        
        public IServiceProvider? Services => serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                InitializeServices();
                
                var loginWindow = new LoginWindow();
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult == true && loginWindow.LoggedInPlayer != null)
                {
                    var mainWindow = new MainWindow(loginWindow.LoggedInPlayer);
                    mainWindow.Show();
                    Application.Current.MainWindow = mainWindow;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup error: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeServices()
        {
            try
            {
                var services = new ServiceCollection();
                
                string currentDirectory = Directory.GetCurrentDirectory();
                string dbPath = Path.Combine(currentDirectory, "Connect4ClientDb.mdf");
                string connectionString = $"Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename={dbPath};Database=Connect4ClientDb;Trusted_Connection=True;MultipleActiveResultSets=true";
                
                services.AddDbContext<GameContext>(options =>
                    options.UseSqlServer(connectionString));
                
                serviceProvider = services.BuildServiceProvider();
                
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                
                context.Database.CanConnect();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Service initialization error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
} 