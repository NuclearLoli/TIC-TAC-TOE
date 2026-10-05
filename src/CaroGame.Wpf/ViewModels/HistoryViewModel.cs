using System.Collections.ObjectModel;
using CaroGame.Data.Entities;
using CaroGame.Data.Repositories;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IGameRepository _gameRepository;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private GameEntity? _selectedGame;

    public ObservableCollection<GameEntity> Games { get; } = new();

    public HistoryViewModel(
        INavigationService navigationService,
        IGameRepository gameRepository,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _gameRepository = gameRepository;
        _dialogService = dialogService;

        _ = LoadGamesAsync();
    }

    [RelayCommand]
    public async Task LoadGamesAsync()
    {
        IsLoading = true;
        Games.Clear();

        try
        {
            var list = await _gameRepository.GetRecentGamesAsync(limit: 50);
            foreach (var game in list)
            {
                Games.Add(game);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ReplayGame(GameEntity? game)
    {
        game ??= SelectedGame;
        if (game == null) return;

        _navigationService.NavigateToReplay(game.Id);
    }

    [RelayCommand]
    public async Task DeleteGame(GameEntity? game)
    {
        game ??= SelectedGame;
        if (game == null) return;

        if (_dialogService.ShowConfirmation("Xóa ván đấu", "Bạn có chắc chắn muốn xóa bản ghi ván đấu này không?"))
        {
            await _gameRepository.DeleteGameAsync(game.Id);
            Games.Remove(game);
        }
    }

    [RelayCommand]
    public void BackToMenu()
    {
        _navigationService.NavigateToMenu();
    }
}
