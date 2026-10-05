using System.Collections.ObjectModel;
using System.Windows.Threading;
using CaroGame.Core.AI;
using CaroGame.Core.Enums;
using CaroGame.Core.History;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using CaroGame.Core.Rules;
using CaroGame.Data.Entities;
using CaroGame.Data.Repositories;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class GamePlayViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ISoundService _soundService;
    private readonly IGameRepository _gameRepository;
    private readonly IAiEngine _aiEngine;
    private readonly INetworkService _networkService;
    private readonly GameSettings _settings;
    private readonly MoveHistoryManager _historyManager = new();
    private readonly IRuleEngine _ruleEngine;

    private readonly DispatcherTimer _timer = new();
    private DateTime _gameStartTime;
    private CancellationTokenSource? _aiCts;

    [ObservableProperty]
    private DynamicBoard _board;

    [ObservableProperty]
    private CellState _currentPlayer = CellState.X;

    [ObservableProperty]
    private Coordinate? _lastMove;

    [ObservableProperty]
    private IReadOnlyList<Coordinate>? _winningLine;

    [ObservableProperty]
    private GameState _gameState = GameState.Playing;

    [ObservableProperty]
    private string _statusMessage = "Lượt của X";

    [ObservableProperty]
    private int _remainingSeconds;

    [ObservableProperty]
    private int _totalElapsedSeconds;

    [ObservableProperty]
    private string _formattedTotalTime = "00:00";

    [ObservableProperty]
    private bool _isTurnTimerEnabled;

    [ObservableProperty]
    private bool _isAiThinking;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private int _moveCount;

    [ObservableProperty]
    private string _modeTitle = string.Empty;

    [ObservableProperty]
    private bool _isOnlineMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OpponentOnlineRole))]
    private CellState _myOnlineRole = CellState.Empty;

    public CellState OpponentOnlineRole => MyOnlineRole == CellState.X ? CellState.O : (MyOnlineRole == CellState.O ? CellState.X : CellState.Empty);

    [ObservableProperty]
    private bool _isRematchRequestedByOpponent;

    [ObservableProperty]
    private bool _isWaitingForRematch;

    // Chat & Online Profile
    public ObservableCollection<ChatMessageDto> ChatMessages { get; } = new();

    [ObservableProperty]
    private string _newChatMessage = string.Empty;

    [ObservableProperty]
    private bool _isChatOpen;

    [ObservableProperty]
    private int _unreadChatCount;

    [ObservableProperty]
    private string _myDisplayName = "Tôi";

    [ObservableProperty]
    private int _myElo = 1000;

    [ObservableProperty]
    private string _myAvatar = "king";

    [ObservableProperty]
    private string _opponentDisplayName = "Đối thủ";

    [ObservableProperty]
    private int _opponentElo = 1000;

    [ObservableProperty]
    private string _opponentAvatar = "knight";

    public GamePlayViewModel(
        INavigationService navigationService,
        ISoundService soundService,
        IGameRepository gameRepository,
        IAiEngine aiEngine,
        INetworkService networkService,
        GameSettings settings)
    {
        _navigationService = navigationService;
        _soundService = soundService;
        _gameRepository = gameRepository;
        _aiEngine = aiEngine;
        _networkService = networkService;
        _settings = settings;

        _ruleEngine = settings.Rule == RuleType.BlockedBothEnds
            ? new VietnameseRuleEngine()
            : new FreeRuleEngine();

        _board = new DynamicBoard(initialHalfSize: settings.InitialHalfSize);
        _historyManager.HistoryChanged += UpdateHistoryProperties;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        IsOnlineMode = _settings.Mode == GameMode.Online;
        if (IsOnlineMode)
        {
            MyOnlineRole = _networkService.MyRole;
            if (_networkService.CurrentUser != null)
            {
                MyDisplayName = _networkService.CurrentUser.DisplayName;
                MyElo = _networkService.CurrentUser.EloRating;
            }

            _networkService.MoveReceived += OnNetworkMoveReceived;
            _networkService.ChatMessageReceived += OnNetworkChatMessageReceived;
            _networkService.MatchFinished += OnNetworkMatchFinished;
            _networkService.OpponentLeft += OnNetworkOpponentLeft;
            _networkService.RematchRequested += OnNetworkRematchRequested;
            _networkService.RematchStarted += OnNetworkRematchStarted;
        }

        ResetGameState();
    }

    private void UpdateHistoryProperties()
    {
        CanUndo = !IsOnlineMode && _historyManager.CanUndo && !IsAiThinking && GameState == GameState.Playing;
        CanRedo = !IsOnlineMode && _historyManager.CanRedo && !IsAiThinking && GameState == GameState.Playing;
        MoveCount = _historyManager.MoveCount;
    }

    [RelayCommand]
    public void ToggleChat()
    {
        IsChatOpen = !IsChatOpen;
        if (IsChatOpen)
        {
            UnreadChatCount = 0;
        }
    }

    [RelayCommand]
    public async Task SendChatMessage()
    {
        if (string.IsNullOrWhiteSpace(NewChatMessage)) return;

        string msg = NewChatMessage.Trim();
        NewChatMessage = string.Empty;
        await _networkService.SendChatMessageAsync(msg);
    }

    private void OnNetworkChatMessageReceived(ChatMessageDto msg)
    {
        ChatMessages.Add(msg);
        if (!IsChatOpen)
        {
            UnreadChatCount++;
        }
        _soundService.Play(SoundEffectType.ChatMessage);
    }

    private void OnNetworkMatchFinished(MatchFinishDto dto)
    {
        if (IsOnlineMode)
        {
            string eloDiff = MyOnlineRole == CellState.X
                ? (dto.EloChangeX >= 0 ? $"+{dto.EloChangeX}" : $"{dto.EloChangeX}")
                : (dto.EloChangeO >= 0 ? $"+{dto.EloChangeO}" : $"{dto.EloChangeO}");

            int newElo = MyOnlineRole == CellState.X ? dto.NewEloX : dto.NewEloO;
            MyElo = newElo;

            StatusMessage = $"{dto.WinnerName} thắng! ({eloDiff} Elo • Điểm mới: {newElo})";
        }
    }

    [RelayCommand]
    public async Task StartNewGame()
    {
        if (IsOnlineMode)
        {
            if (GameState != GameState.Playing)
            {
                IsWaitingForRematch = true;
                StatusMessage = "Đang gửi yêu cầu đấu lại tới đối thủ...";
                await _networkService.RequestRematchAsync();
                return;
            }
        }

        ResetGameState();
    }

    private void ResetGameState()
    {
        _aiCts?.Cancel();
        Board.Reset(_settings.InitialHalfSize);
        _historyManager.Clear();

        CurrentPlayer = CellState.X;
        LastMove = null;
        WinningLine = null;
        GameState = GameState.Playing;
        IsAiThinking = false;
        IsRematchRequestedByOpponent = false;
        IsWaitingForRematch = false;
        _gameStartTime = DateTime.UtcNow;
        TotalElapsedSeconds = 0;
        FormattedTotalTime = "00:00";
        IsTurnTimerEnabled = _settings.EnableTimer;

        if (IsOnlineMode)
        {
            if (_networkService.CurrentGameInfo != null)
            {
                var info = _networkService.CurrentGameInfo;
                if (MyOnlineRole == CellState.X)
                {
                    MyDisplayName = info.HostName;
                    MyElo = info.HostElo;
                    MyAvatar = info.HostAvatar;
                    OpponentDisplayName = info.GuestName;
                    OpponentElo = info.GuestElo;
                    OpponentAvatar = info.GuestAvatar;
                }
                else
                {
                    MyDisplayName = info.GuestName;
                    MyElo = info.GuestElo;
                    MyAvatar = info.GuestAvatar;
                    OpponentDisplayName = info.HostName;
                    OpponentElo = info.HostElo;
                    OpponentAvatar = info.HostAvatar;
                }
            }
            else if (_networkService.CurrentUser != null)
            {
                MyDisplayName = _networkService.CurrentUser.DisplayName;
                MyElo = _networkService.CurrentUser.EloRating;
                MyAvatar = _networkService.CurrentUser.Avatar;
            }

            string rankedText = (_networkService.CurrentGameInfo?.IsRanked ?? true) ? "Xếp hạng" : "Giao hữu";
            string ruleText = _settings.Rule == RuleType.BlockedBothEnds ? "Chặn 2 đầu" : "Tự do";
            ModeTitle = $"Đấu Online ({_networkService.CurrentRoomCode}) • [{rankedText}] • Luật {ruleText}";
            StatusMessage = MyOnlineRole == CellState.X
                ? "Trận đấu bắt đầu! Lượt của bạn (X)"
                : "Trận đấu bắt đầu! Đang đợi đối thủ (X) đánh...";
        }
        else
        {
            ModeTitle = _settings.Mode == GameMode.PvC
                ? $"Đấu với Robot ({_settings.Difficulty}) • Luật {(_settings.Rule == RuleType.BlockedBothEnds ? "Chặn 2 đầu" : "Tự do")}"
                : $"Người đấu với Người • Luật {(_settings.Rule == RuleType.BlockedBothEnds ? "Chặn 2 đầu" : "Tự do")}";

            StatusMessage = "Trận đấu bắt đầu! Lượt của bạn (X)";
        }

        ResetTurnTimer();
        UpdateHistoryProperties();

        _timer.Stop();
        _timer.Start();
    }

    private void ResetTurnTimer()
    {
        if (_settings.EnableTimer)
        {
            RemainingSeconds = _settings.TurnTimeLimitSeconds;
        }
        else
        {
            RemainingSeconds = 0;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (GameState != GameState.Playing)
        {
            _timer.Stop();
            return;
        }

        TotalElapsedSeconds = (int)(DateTime.UtcNow - _gameStartTime).TotalSeconds;
        int minutes = TotalElapsedSeconds / 60;
        int seconds = TotalElapsedSeconds % 60;
        FormattedTotalTime = $"{minutes:D2}:{seconds:D2}";

        if (_settings.EnableTimer)
        {
            if (RemainingSeconds > 0)
            {
                RemainingSeconds--;
                if (RemainingSeconds <= 5 && RemainingSeconds > 0)
                {
                    _soundService.Play(SoundEffectType.TimerTick);
                }
            }
            else
            {
                CellState opponent = CurrentPlayer == CellState.X ? CellState.O : CellState.X;
                HandleGameOver(opponent, "Hết thời gian lượt đi!");
            }
        }
    }

    [RelayCommand]
    public async Task CellClicked(Coordinate coord)
    {
        if (GameState != GameState.Playing || IsAiThinking) return;
        if (!Board.IsEmpty(coord)) return;

        if (IsOnlineMode)
        {
            if (CurrentPlayer != MyOnlineRole) return;

            ExecuteMove(coord, CurrentPlayer);
            await _networkService.SendMoveAsync(coord, MyOnlineRole, _historyManager.MoveCount);
            return;
        }

        // Offline logic
        ExecuteMove(coord, CurrentPlayer);

        if (GameState != GameState.Playing) return;

        if (_settings.Mode == GameMode.PvC && CurrentPlayer == CellState.O)
        {
            await TriggerAiMoveAsync();
        }
    }

    private void ExecuteMove(Coordinate coord, CellState player)
    {
        Board.SetCell(coord, player);
        _historyManager.RecordMove(coord, player);
        LastMove = coord;
        _soundService.Play(SoundEffectType.MovePlaced);

        WinResult win = _ruleEngine.CheckWin(Board, coord);
        if (win.HasWon)
        {
            WinningLine = win.WinningLine;
            HandleGameOver(win.Winner, $"{win.Winner} đã giành chiến thắng!");
            if (IsOnlineMode && win.Winner == MyOnlineRole)
            {
                _ = _networkService.ReportGameOverAsync(win.Winner, $"{win.Winner} đã giành chiến thắng!");
            }
            return;
        }

        CurrentPlayer = player == CellState.X ? CellState.O : CellState.X;

        if (IsOnlineMode)
        {
            StatusMessage = CurrentPlayer == MyOnlineRole
                ? $"Đến lượt của bạn ({MyOnlineRole})"
                : $"Lượt của đối thủ ({CurrentPlayer})...";
        }
        else
        {
            StatusMessage = $"Lượt của {CurrentPlayer}";
        }

        ResetTurnTimer();
        UpdateHistoryProperties();
    }

    private async Task TriggerAiMoveAsync()
    {
        IsAiThinking = true;
        StatusMessage = "Robot đang tính toán...";
        UpdateHistoryProperties();

        _aiCts = new CancellationTokenSource();

        try
        {
            var delayTask = Task.Delay(250, _aiCts.Token);
            var aiMoveTask = _aiEngine.CalculateBestMoveAsync(Board, CellState.O, _settings.Difficulty, _settings.Rule, _aiCts.Token);

            await Task.WhenAll(delayTask, aiMoveTask);
            Coordinate aiCoord = await aiMoveTask;

            if (!_aiCts.Token.IsCancellationRequested && GameState == GameState.Playing)
            {
                ExecuteMove(aiCoord, CellState.O);
            }
        }
        catch (OperationCanceledException)
        {
            // AI cancelled
        }
        finally
        {
            IsAiThinking = false;
            UpdateHistoryProperties();
        }
    }

    private void HandleGameOver(CellState winner, string message)
    {
        _timer.Stop();
        GameState = winner == CellState.X ? GameState.WonX : GameState.WonO;
        StatusMessage = message;

        if (IsOnlineMode)
        {
            if (winner == MyOnlineRole)
            {
                _soundService.Play(SoundEffectType.GameWon);
            }
            else
            {
                _soundService.Play(SoundEffectType.GameLost);
            }
        }
        else
        {
            if (winner == CellState.X)
            {
                _soundService.Play(SoundEffectType.GameWon);
            }
            else
            {
                _soundService.Play(_settings.Mode == GameMode.PvC ? SoundEffectType.GameLost : SoundEffectType.GameWon);
            }
        }

        _ = SaveGameRecordAsync(winner);
        UpdateHistoryProperties();
    }

    private void OnNetworkMoveReceived(NetworkMoveDto move)
    {
        var coord = new Coordinate(move.Row, move.Col);
        if (GameState == GameState.Playing && Board.IsEmpty(coord))
        {
            ExecuteMove(coord, move.Player);
        }
    }

    private void OnNetworkOpponentLeft(string reason)
    {
        if (GameState == GameState.Playing)
        {
            HandleGameOver(MyOnlineRole, $"{reason} Bạn giành chiến thắng!");
        }
        else
        {
            StatusMessage = reason;
        }
    }

    private void OnNetworkRematchRequested()
    {
        IsRematchRequestedByOpponent = true;
        StatusMessage = "Đối thủ yêu cầu đấu lại! Bấm [Chấp nhận đấu lại] để bắt đầu.";
    }

    private void OnNetworkRematchStarted(GameStartDto dto)
    {
        MyOnlineRole = dto.YourRole;
        IsRematchRequestedByOpponent = false;
        IsWaitingForRematch = false;
        ResetGameState();
    }

    [RelayCommand]
    public async Task AcceptRematch()
    {
        if (IsOnlineMode)
        {
            await _networkService.AcceptRematchAsync();
        }
    }

    [RelayCommand]
    public async Task Surrender()
    {
        if (GameState != GameState.Playing) return;

        if (IsOnlineMode)
        {
            CellState winner = MyOnlineRole == CellState.X ? CellState.O : CellState.X;
            await _networkService.ReportGameOverAsync(winner, "Đối thủ đã đầu hàng.");
            await _networkService.LeaveRoomAsync();
            HandleGameOver(winner, "Bạn đã đầu hàng. Đối thủ giành chiến thắng!");
        }
        else
        {
            CellState winner = CurrentPlayer == CellState.X ? CellState.O : CellState.X;
            HandleGameOver(winner, $"{CurrentPlayer} đã đầu hàng.");
        }
    }

    private async Task SaveGameRecordAsync(CellState winner)
    {
        try
        {
            var moves = _historyManager.AllMoves.Select(m => new MoveEntity
            {
                TurnNumber = m.TurnNumber,
                Player = m.Player.ToString(),
                Row = m.Coord.Row,
                Col = m.Coord.Col,
                Timestamp = m.Timestamp
            }).ToList();

            var gameEntity = new GameEntity
            {
                PlayedAt = _gameStartTime,
                GameMode = _settings.Mode.ToString(),
                AiDifficulty = _settings.Mode == GameMode.PvC ? _settings.Difficulty.ToString() : null,
                RuleType = _settings.Rule.ToString(),
                Winner = winner.ToString(),
                DurationSeconds = (int)(DateTime.UtcNow - _gameStartTime).TotalSeconds,
                TotalMoves = moves.Count
            };

            await _gameRepository.SaveGameAsync(gameEntity, moves);
        }
        catch
        {
            // Do not break UI if save fails
        }
    }

    [RelayCommand]
    public void Undo()
    {
        if (!CanUndo || IsOnlineMode) return;

        if (_settings.Mode == GameMode.PvC)
        {
            _historyManager.Undo(Board);
            _historyManager.Undo(Board);
            CurrentPlayer = CellState.X;
            LastMove = _historyManager.AllMoves.LastOrDefault()?.Coord;
        }
        else
        {
            _historyManager.Undo(Board);
            CurrentPlayer = CurrentPlayer == CellState.X ? CellState.O : CellState.X;
            LastMove = _historyManager.AllMoves.LastOrDefault()?.Coord;
        }

        WinningLine = null;
        GameState = GameState.Playing;
        StatusMessage = $"Lượt của {CurrentPlayer}";
        _soundService.Play(SoundEffectType.UndoMove);
        ResetTurnTimer();
        UpdateHistoryProperties();
    }

    [RelayCommand]
    public void Redo()
    {
        if (!CanRedo || IsOnlineMode) return;

        if (_settings.Mode == GameMode.PvC)
        {
            _historyManager.Redo(Board);
            var lastAi = _historyManager.Redo(Board);
            CurrentPlayer = CellState.X;
            LastMove = lastAi?.Coord;
        }
        else
        {
            var redone = _historyManager.Redo(Board);
            CurrentPlayer = CurrentPlayer == CellState.X ? CellState.O : CellState.X;
            LastMove = redone?.Coord;
        }

        StatusMessage = $"Lượt của {CurrentPlayer}";
        _soundService.Play(SoundEffectType.MovePlaced);
        ResetTurnTimer();
        UpdateHistoryProperties();
    }

    [RelayCommand]
    public void BackToMenu()
    {
        _aiCts?.Cancel();
        _timer.Stop();

        if (IsOnlineMode)
        {
            _networkService.MoveReceived -= OnNetworkMoveReceived;
            _networkService.ChatMessageReceived -= OnNetworkChatMessageReceived;
            _networkService.MatchFinished -= OnNetworkMatchFinished;
            _networkService.OpponentLeft -= OnNetworkOpponentLeft;
            _networkService.RematchRequested -= OnNetworkRematchRequested;
            _networkService.RematchStarted -= OnNetworkRematchStarted;
            _ = _networkService.LeaveRoomAsync();
        }

        _navigationService.NavigateToMenu();
    }
}
