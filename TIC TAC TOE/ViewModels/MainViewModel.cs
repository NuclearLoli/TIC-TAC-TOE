using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TIC_TAC_TOE.AI;
using TIC_TAC_TOE.Commands;
using TIC_TAC_TOE.Models;
using TIC_TAC_TOE.Services;

namespace TIC_TAC_TOE.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly BackgroundImageService _backgroundImageService;
    private readonly GameService _gameService;
    private readonly GameHistoryService _gameHistoryService;
    private readonly MoveFinder _moveFinder;
    private Brush _appBackgroundBrush;
    private string _statusText = string.Empty;
    private string _currentPlayerText = string.Empty;
    private string _gameModeText = string.Empty;
    private string _difficultyText = string.Empty;
    private string _backgroundName = "Nền mặc định";
    private string _historyStatusText = "Lịch sử trận đấu chưa được tải.";
    private GameMode _selectedGameMode;
    private AiDifficulty _selectedDifficulty;
    private bool _isComputerThinking;
    private bool _isInitializing;
    private bool _hasSavedCurrentGame;
    private bool _isInGame;
    private WindowState _currentWindowState = WindowState.Normal;

    public MainViewModel()
    {
        _backgroundImageService = new BackgroundImageService();
        _gameService = new GameService();
        _gameHistoryService = new GameHistoryService();
        _moveFinder = new MoveFinder(new BoardEvaluator());
        _appBackgroundBrush = CreateDefaultBackgroundBrush();

        Cells = new ObservableCollection<CellViewModel>();
        RecentGames = new ObservableCollection<GameHistorySummary>();

        StartPvPCommand = new RelayCommand(StartPvPMode);
        StartPvCCommand = new RelayCommand(StartPvCMode);
        BackToMenuCommand = new RelayCommand(BackToMenu);
        ChooseBackgroundCommand = new RelayCommand(ChooseBackgroundImage);
        ClearBackgroundCommand = new RelayCommand(ClearBackgroundImage);
        ToggleWindowModeCommand = new RelayCommand(ToggleWindowMode);
        NewGameCommand = new RelayCommand(StartNewGame);
        RestartCommand = new RelayCommand(RestartGame);

        Title = "Caro trực tuyến";
        Subtitle = "Người đầu tiên nối được năm quân cờ sẽ thắng.";
        AvailableDifficulties = new List<AiDifficulty>
        {
            AiDifficulty.De,
            AiDifficulty.Vua,
            AiDifficulty.SieuKho
        };

        CreateBoard();

        _isInitializing = true;
        SelectedGameMode = GameMode.PvP;
        SelectedDifficulty = AiDifficulty.Vua;
        _isInitializing = false;

        BackToMenu();
        _ = LoadRecentGamesAsync();
    }

    public ObservableCollection<CellViewModel> Cells { get; }

    public ObservableCollection<GameHistorySummary> RecentGames { get; }

    public IReadOnlyList<AiDifficulty> AvailableDifficulties { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public Brush AppBackgroundBrush
    {
        get => _appBackgroundBrush;
        private set => SetProperty(ref _appBackgroundBrush, value);
    }

    public WindowState CurrentWindowState
    {
        get => _currentWindowState;
        set
        {
            if (!SetProperty(ref _currentWindowState, value))
            {
                return;
            }

            OnPropertyChanged(nameof(WindowModeButtonText));
        }
    }

    public string WindowModeButtonText =>
        CurrentWindowState == WindowState.Maximized ? "Cửa sổ" : "Toàn màn hình";

    public string GameModeText
    {
        get => _gameModeText;
        private set => SetProperty(ref _gameModeText, value);
    }

    public string DifficultyText
    {
        get => _difficultyText;
        private set => SetProperty(ref _difficultyText, value);
    }

    public string BackgroundName
    {
        get => _backgroundName;
        private set => SetProperty(ref _backgroundName, value);
    }

    public GameMode SelectedGameMode
    {
        get => _selectedGameMode;
        set
        {
            if (!SetProperty(ref _selectedGameMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsPvPMode));
            OnPropertyChanged(nameof(IsPvCMode));
            OnPropertyChanged(nameof(IsDifficultySelectionEnabled));

            if (!_isInitializing && IsInGame)
            {
                ResetGame();
            }
            else
            {
                RefreshBoardState();
            }
        }
    }

    public AiDifficulty SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (!SetProperty(ref _selectedDifficulty, value))
            {
                return;
            }

            if (!_isInitializing && SelectedGameMode == GameMode.PvC && IsInGame)
            {
                ResetGame();
            }
            else
            {
                RefreshBoardState();
            }
        }
    }

    public bool IsPvPMode => SelectedGameMode == GameMode.PvP;

    public bool IsPvCMode => SelectedGameMode == GameMode.PvC;

    public bool IsDifficultySelectionEnabled => IsPvCMode;

    public bool IsInGame
    {
        get => _isInGame;
        private set
        {
            if (!SetProperty(ref _isInGame, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsMenuVisible));
        }
    }

    public bool IsMenuVisible => !IsInGame;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string HistoryStatusText
    {
        get => _historyStatusText;
        private set => SetProperty(ref _historyStatusText, value);
    }

    public string CurrentPlayerText
    {
        get => _currentPlayerText;
        private set => SetProperty(ref _currentPlayerText, value);
    }

    public int MoveCountX => _gameService.Moves.Count(move => move.Player == CellState.PlayerX);

    public int MoveCountO => _gameService.Moves.Count(move => move.Player == CellState.PlayerO);

    public ICommand StartPvPCommand { get; }

    public ICommand StartPvCCommand { get; }

    public ICommand BackToMenuCommand { get; }

    public ICommand ChooseBackgroundCommand { get; }

    public ICommand ClearBackgroundCommand { get; }

    public ICommand ToggleWindowModeCommand { get; }

    public ICommand NewGameCommand { get; }

    public ICommand RestartCommand { get; }

    private void StartPvPMode()
    {
        SelectedGameMode = GameMode.PvP;
        IsInGame = true;
        ResetGame();
    }

    private void StartPvCMode()
    {
        SelectedGameMode = GameMode.PvC;
        IsInGame = true;
        ResetGame();
    }

    private void BackToMenu()
    {
        IsInGame = false;
        _isComputerThinking = false;
        _hasSavedCurrentGame = false;
        StatusText = "Chọn chế độ chơi để bắt đầu ván mới.";
        CurrentPlayerText = "Sẵn sàng bắt đầu";
        GameModeText = "Chưa chọn chế độ";
        DifficultyText = string.Empty;
        RefreshBoardCells();
        OnPropertyChanged(nameof(MoveCountX));
        OnPropertyChanged(nameof(MoveCountO));
    }

    private void ToggleWindowMode()
    {
        CurrentWindowState = CurrentWindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void ChooseBackgroundImage()
    {
        string? filePath = _backgroundImageService.PickImageFile();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        AppBackgroundBrush = _backgroundImageService.CreateBackgroundBrush(filePath);
        BackgroundName = _backgroundImageService.GetDisplayName(filePath);
    }

    private void ClearBackgroundImage()
    {
        AppBackgroundBrush = CreateDefaultBackgroundBrush();
        BackgroundName = "Nền mặc định";
    }

    private void StartNewGame()
    {
        ResetGame();
    }

    private void RestartGame()
    {
        ResetGame();
    }

    private void CreateBoard()
    {
        Cells.Clear();

        for (int row = 0; row < Board.Size; row++)
        {
            for (int column = 0; column < Board.Size; column++)
            {
                Cells.Add(new CellViewModel(row, column, HandleCellPlayed));
            }
        }
    }

    private async void HandleCellPlayed(int row, int column)
    {
        if (!IsInGame || _isComputerThinking)
        {
            return;
        }

        bool success = _gameService.MakeMove(row, column);

        if (!success)
        {
            StatusText = _gameService.MoveCount == 0
                ? "Bạn có thể đặt nước đầu tiên ở bất kỳ ô trống nào."
                : "Nước đi không hợp lệ. Bạn chỉ được đánh vào ô trống liền kề quân đã có.";
            RefreshBoardCells();
            return;
        }

        RefreshBoardState();
        await SaveGameIfNeededAsync();

        if (IsPvCMode &&
            !_gameService.IsGameOver &&
            _gameService.CurrentPlayer == CellState.PlayerO)
        {
            _isComputerThinking = true;
            RefreshBoardState();

            await Task.Delay(180);
            ExecuteComputerMove();

            _isComputerThinking = false;
            RefreshBoardState();
            await SaveGameIfNeededAsync();
        }
    }

    private void ResetGame()
    {
        _gameService.Restart();
        _isComputerThinking = false;
        _hasSavedCurrentGame = false;
        RefreshBoardState();
    }

    private void ExecuteComputerMove()
    {
        (int row, int column) = _moveFinder.FindBestMove(
            _gameService.Board,
            CellState.PlayerO,
            SelectedDifficulty);

        _gameService.MakeMove(row, column);
    }

    private void RefreshBoardState()
    {
        RefreshBoardCells();

        GameModeText = IsPvPMode
            ? "PvP local"
            : "PvC - Người chơi là X, Computer là O";

        DifficultyText = IsPvCMode
            ? GetDifficultyDisplayName(SelectedDifficulty)
            : string.Empty;

        if (_gameService.IsGameOver)
        {
            CurrentPlayerText = "Ván đấu đã kết thúc";
            StatusText = _gameService.Result switch
            {
                GameResult.PlayerXWin => "Kết quả: X thắng.",
                GameResult.PlayerOWin => "Kết quả: O thắng.",
                GameResult.Draw => "Kết quả: Hòa. Bàn cờ đã đầy.",
                _ => "Kết quả: Không xác định."
            };
        }
        else
        {
            CurrentPlayerText = _isComputerThinking
                ? $"Computer đang suy nghĩ ({GetDifficultyDisplayName(SelectedDifficulty)})..."
                : _gameService.CurrentPlayer == CellState.PlayerX
                    ? "X"
                    : IsPvCMode
                        ? "Computer (O)"
                        : "O";

            StatusText = _gameService.MoveCount == 0
                ? "Bạn có thể đặt nước đầu tiên ở bất kỳ ô trống nào."
                : "Chỉ được đánh vào các ô trống liền kề quân đã có.";
        }

        OnPropertyChanged(nameof(MoveCountX));
        OnPropertyChanged(nameof(MoveCountO));
    }

    private void RefreshBoardCells()
    {
        bool canPlayAnyCell = IsInGame && !_gameService.IsGameOver && !_isComputerThinking;

        foreach (CellViewModel cell in Cells)
        {
            bool isLegalTarget = canPlayAnyCell && _gameService.CanMakeMove(cell.Row, cell.Column);
            CellState cellState = _gameService.Board.GetCell(cell.Row, cell.Column);
            cell.RefreshState(cellState, isLegalTarget);
        }
    }

    private async Task SaveGameIfNeededAsync()
    {
        if (!_gameService.IsGameOver || _hasSavedCurrentGame)
        {
            return;
        }

        _hasSavedCurrentGame = true;

        try
        {
            await _gameHistoryService.SaveCompletedGameAsync(
                SelectedGameMode,
                IsPvCMode ? SelectedDifficulty : null,
                _gameService.Result,
                _gameService.StartedAt,
                _gameService.Moves);

            HistoryStatusText = "Đã lưu trận đấu vào SQL Server thành công.";
            await LoadRecentGamesAsync();
        }
        catch (Exception exception)
        {
            _hasSavedCurrentGame = false;
            HistoryStatusText = $"Không thể lưu lịch sử trận đấu: {exception.Message}";
        }
    }

    private async Task LoadRecentGamesAsync()
    {
        try
        {
            IReadOnlyList<GameHistorySummary> games = await _gameHistoryService.GetRecentGamesAsync();

            RecentGames.Clear();

            foreach (GameHistorySummary game in games)
            {
                RecentGames.Add(game);
            }

            if (games.Count == 0)
            {
                HistoryStatusText = "Chưa có trận đấu nào được lưu trong cơ sở dữ liệu.";
            }
            else if (!_gameService.IsGameOver)
            {
                HistoryStatusText = $"Đã tải {games.Count} trận gần nhất.";
            }
        }
        catch (Exception exception)
        {
            HistoryStatusText = $"Không thể tải lịch sử trận đấu: {exception.Message}";
        }
    }

    private static Brush CreateDefaultBackgroundBrush()
    {
        LinearGradientBrush brush = new();
        brush.StartPoint = new System.Windows.Point(0, 0);
        brush.EndPoint = new System.Windows.Point(0, 1);
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 250, 243), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(247, 240, 228), 0.58));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(240, 232, 219), 1));
        brush.Freeze();
        return brush;
    }

    private static string GetDifficultyDisplayName(AiDifficulty difficulty)
    {
        return difficulty switch
        {
            AiDifficulty.De => "Dễ",
            AiDifficulty.Vua => "Vừa",
            _ => "Siêu khó"
        };
    }
}
