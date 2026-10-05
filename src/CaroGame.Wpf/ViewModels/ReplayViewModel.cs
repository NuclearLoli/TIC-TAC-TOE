using System.Windows.Threading;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Data.Entities;
using CaroGame.Data.Repositories;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class ReplayViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IGameRepository _gameRepository;
    private readonly ISoundService _soundService;
    private readonly int _gameId;

    private List<MoveEntity> _moves = new();
    private int _currentStepIndex = -1;
    private readonly DispatcherTimer _autoPlayTimer = new();

    [ObservableProperty]
    private DynamicBoard _board;

    [ObservableProperty]
    private Coordinate? _lastMove;

    [ObservableProperty]
    private string _gameInfoText = string.Empty;

    [ObservableProperty]
    private string _stepInfoText = "Nước: 0 / 0";

    [ObservableProperty]
    private bool _canGoPrevious;

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _isAutoPlaying;

    [ObservableProperty]
    private double _playSpeedSeconds = 1.0;

    public ReplayViewModel(
        int gameId,
        INavigationService navigationService,
        IGameRepository gameRepository,
        ISoundService soundService)
    {
        _gameId = gameId;
        _navigationService = navigationService;
        _gameRepository = gameRepository;
        _soundService = soundService;

        _board = new DynamicBoard(initialHalfSize: 7);
        _autoPlayTimer.Tick += AutoPlayTimer_Tick;

        _ = LoadReplayAsync();
    }

    private async Task LoadReplayAsync()
    {
        GameEntity? game = await _gameRepository.GetGameWithMovesAsync(_gameId);
        if (game == null)
        {
            GameInfoText = "Không tìm thấy dữ liệu ván đấu.";
            return;
        }

        _moves = game.Moves.OrderBy(m => m.TurnNumber).ToList();
        GameInfoText = $"{game.GameMode} | Người thắng: {game.Winner} | {game.PlayedAt.ToLocalTime():yyyy-MM-dd HH:mm}";

        Board.Reset(initialHalfSize: 7);
        _currentStepIndex = -1;
        UpdateControls();
    }

    private void UpdateControls()
    {
        CanGoPrevious = _currentStepIndex >= 0;
        CanGoNext = _currentStepIndex < _moves.Count - 1;
        StepInfoText = $"Nước: {(_currentStepIndex + 1)} / {_moves.Count}";
    }

    [RelayCommand]
    public void NextMove()
    {
        if (_currentStepIndex >= _moves.Count - 1) return;

        _currentStepIndex++;
        MoveEntity move = _moves[_currentStepIndex];
        CellState player = move.Player == "X" ? CellState.X : CellState.O;
        Coordinate coord = new(move.Row, move.Col);

        Board.SetCell(coord, player);
        LastMove = coord;
        _soundService.Play(SoundEffectType.MovePlaced);

        UpdateControls();
    }

    [RelayCommand]
    public void PreviousMove()
    {
        if (_currentStepIndex < 0) return;

        MoveEntity move = _moves[_currentStepIndex];
        Coordinate coord = new(move.Row, move.Col);
        Board.RemoveCell(coord);

        _currentStepIndex--;
        LastMove = _currentStepIndex >= 0 ? new Coordinate(_moves[_currentStepIndex].Row, _moves[_currentStepIndex].Col) : null;

        UpdateControls();
    }

    [RelayCommand]
    public void ToggleAutoPlay()
    {
        if (IsAutoPlaying)
        {
            _autoPlayTimer.Stop();
            IsAutoPlaying = false;
        }
        else
        {
            if (_currentStepIndex >= _moves.Count - 1)
            {
                // Reset về đầu để xem lại
                Board.Reset(7);
                _currentStepIndex = -1;
                LastMove = null;
            }

            _autoPlayTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.2, PlaySpeedSeconds));
            _autoPlayTimer.Start();
            IsAutoPlaying = true;
        }
    }

    private void AutoPlayTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentStepIndex < _moves.Count - 1)
        {
            NextMove();
        }
        else
        {
            _autoPlayTimer.Stop();
            IsAutoPlaying = false;
        }
    }

    [RelayCommand]
    public void BackToHistory()
    {
        _autoPlayTimer.Stop();
        _navigationService.NavigateToHistory();
    }
}
