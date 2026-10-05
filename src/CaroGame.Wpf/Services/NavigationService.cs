using CaroGame.Core.AI;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using CaroGame.Data.Repositories;
using CaroGame.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CaroGame.Wpf.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private MainViewModel? _mainViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void NavigateToMenu()
    {
        var vm = _serviceProvider.GetRequiredService<MenuViewModel>();
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToGamePlay()
    {
        var vm = new GamePlayViewModel(
            this,
            _serviceProvider.GetRequiredService<ISoundService>(),
            _serviceProvider.GetRequiredService<IGameRepository>(),
            _serviceProvider.GetRequiredService<IAiEngine>(),
            _serviceProvider.GetRequiredService<INetworkService>(),
            _serviceProvider.GetRequiredService<GameSettings>());

        _mainViewModel?.SetView(vm);
    }

    public void NavigateToSettings()
    {
        var vm = _serviceProvider.GetRequiredService<SettingsViewModel>();
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToHistory()
    {
        var vm = _serviceProvider.GetRequiredService<HistoryViewModel>();
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToReplay(int gameId)
    {
        var vm = new ReplayViewModel(
            gameId,
            this,
            _serviceProvider.GetRequiredService<IGameRepository>(),
            _serviceProvider.GetRequiredService<ISoundService>());

        _mainViewModel?.SetView(vm);
    }

    public void NavigateToOnlineLobby()
    {
        var vm = _serviceProvider.GetRequiredService<OnlineLobbyViewModel>();
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToAuth(bool returnToMenu = false, bool isRegister = false)
    {
        var vm = _serviceProvider.GetRequiredService<AuthViewModel>();
        vm.ReturnToMenu = returnToMenu;
        vm.IsLoginMode = !isRegister;
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToLeaderboard()
    {
        var vm = _serviceProvider.GetRequiredService<LeaderboardViewModel>();
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToProfile(Guid? targetUserId = null)
    {
        var vm = new ProfileViewModel(this, _serviceProvider.GetRequiredService<INetworkService>(), targetUserId);
        _mainViewModel?.SetView(vm);
    }

    public void NavigateToFriends()
    {
        var vm = _serviceProvider.GetRequiredService<FriendsViewModel>();
        _mainViewModel?.SetView(vm);
    }
}
