using System.Windows;
using CaroGame.Core.AI;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using CaroGame.Data;
using CaroGame.Data.Repositories;
using CaroGame.Wpf.Services;
using CaroGame.Wpf.ViewModels;
using CaroGame.Wpf.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaroGame.Wpf;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Ensure SQLite database exists
            var repo = _serviceProvider.GetRequiredService<IGameRepository>();
            await repo.EnsureDatabaseCreatedAsync();

            // Setup navigation and initial view
            var nav = (NavigationService)_serviceProvider.GetRequiredService<INavigationService>();
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            nav.SetMainViewModel(mainVm);

            var network = _serviceProvider.GetRequiredService<INetworkService>();
            if (network.CurrentUser != null)
            {
                nav.NavigateToMenu();
            }
            else
            {
                nav.NavigateToAuth(returnToMenu: true);
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi động ứng dụng:\n{ex.Message}\n\nChi tiết:\n{ex}", "Caro Game Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core & Settings
        services.AddSingleton<GameSettings>();
        services.AddSingleton<IAiEngine, MinimaxAiEngine>();

        // Data / SQLite in LocalApplicationData for safety in Program Files and SingleFile publish
        string appDataDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroGame");
        System.IO.Directory.CreateDirectory(appDataDir);
        string dbPath = System.IO.Path.Combine(appDataDir, "caro_game.db");
        services.AddDbContextFactory<CaroDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddSingleton<IGameRepository, SqliteGameRepository>();

        // Services
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INetworkService, SignalRNetworkService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MenuViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<OnlineLobbyViewModel>();
        services.AddTransient<AuthViewModel>();
        services.AddTransient<LeaderboardViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<FriendsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
