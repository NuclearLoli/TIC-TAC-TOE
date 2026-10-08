using System.Windows;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class OnlineLobbyViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;
    private readonly GameSettings _settings;
    private readonly ISoundService _soundService;

    [ObservableProperty]
    private string _roomCode = string.Empty;

    [ObservableProperty]
    private string _joinCode = string.Empty;

    [ObservableProperty]
    private string _serverUrl = "https://carogame-d2bafxdybvg3grae.japaneast-01.azurewebsites.net/carohub";

    [ObservableProperty]
    private bool _isHosting;

    [ObservableProperty]
    private bool _isJoining;

    [ObservableProperty]
    private string _statusMessage = "Tạo phòng và gửi mã cho bạn bè hoặc nhập mã phòng để vào chơi.";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVietnameseRule))]
    [NotifyPropertyChangedFor(nameof(IsFreeRule))]
    private RuleType _selectedRule = RuleType.BlockedBothEnds;

    public bool IsVietnameseRule
    {
        get => SelectedRule == RuleType.BlockedBothEnds;
        set { if (value) SelectedRule = RuleType.BlockedBothEnds; }
    }

    public bool IsFreeRule
    {
        get => SelectedRule == RuleType.FreeRule;
        set { if (value) SelectedRule = RuleType.FreeRule; }
    }

    [ObservableProperty]
    private int _turnTimeLimit = 30;

    [ObservableProperty]
    private bool _isRanked = true;

    public UserProfileDto? CurrentUser => _networkService.CurrentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public string UserDisplayName => !string.IsNullOrWhiteSpace(CurrentUser?.DisplayName)
        ? CurrentUser.DisplayName
        : (!string.IsNullOrWhiteSpace(CurrentUser?.Username) ? CurrentUser.Username : "Khách Chưa Đăng Nhập");
    public string UserAvatarIcon => CurrentUser?.AvatarIcon ?? "👤";
    public string UserFlag => CurrentUser?.CountryFlag ?? "";
    public string UserEloText => CurrentUser != null
        ? $"⭐ {CurrentUser.EloRating} Elo  •  {CurrentUser.Wins} Thắng / {CurrentUser.Losses} Thua ({CurrentUser.WinRate}%)"
        : "Đăng nhập tài khoản để tích lũy Elo và lưu lịch sử đấu";

    public OnlineLobbyViewModel(
        INavigationService navigationService,
        INetworkService networkService,
        GameSettings settings,
        ISoundService soundService)
    {
        _navigationService = navigationService;
        _networkService = networkService;
        _settings = settings;
        _soundService = soundService;

        _selectedRule = settings.Rule;
        _turnTimeLimit = settings.TurnTimeLimitSeconds > 0 ? settings.TurnTimeLimitSeconds : 30;
        _serverUrl = _networkService.ServerUrl;

        GenerateNewRoomCode();

        _networkService.RoomCreated += OnRoomCreated;
        _networkService.GameStarted += OnGameStarted;
        _networkService.ErrorOccurred += OnErrorOccurred;
        _networkService.UserProfileChanged += OnUserProfileChanged;
    }

    private void OnUserProfileChanged(UserProfileDto? profile)
    {
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(UserDisplayName));
        OnPropertyChanged(nameof(UserAvatarIcon));
        OnPropertyChanged(nameof(UserFlag));
        OnPropertyChanged(nameof(UserEloText));
    }

    private void GenerateNewRoomCode()
    {
        var rand = new Random();
        RoomCode = $"CARO-{rand.Next(1000, 9999)}";
    }

    private void OnRoomCreated(string code)
    {
        IsHosting = true;
        IsJoining = false;
        IsError = false;
        StatusMessage = $"Phòng {code} đã sẵn sàng! Đang chờ bạn bè kết nối...";
    }

    private void OnGameStarted(GameStartDto dto)
    {
        UnregisterEvents();

        // Apply settings
        _settings.Mode = GameMode.Online;
        _settings.Rule = dto.Rule;
        _settings.TurnTimeLimitSeconds = dto.TurnTimeLimitSeconds;
        _settings.EnableTimer = dto.TurnTimeLimitSeconds > 0;

        _soundService.Play(SoundEffectType.GameWon);
        _navigationService.NavigateToGamePlay();
    }

    private void OnErrorOccurred(string error)
    {
        IsError = true;
        StatusMessage = error;
        IsHosting = false;
        IsJoining = false;
    }

    private void UnregisterEvents()
    {
        _networkService.RoomCreated -= OnRoomCreated;
        _networkService.GameStarted -= OnGameStarted;
        _networkService.ErrorOccurred -= OnErrorOccurred;
        _networkService.UserProfileChanged -= OnUserProfileChanged;
    }

    [RelayCommand]
    public void OpenAuth()
    {
        UnregisterEvents();
        _navigationService.NavigateToAuth();
    }

    [RelayCommand]
    public void OpenFriends()
    {
        UnregisterEvents();
        _navigationService.NavigateToFriends();
    }

    [RelayCommand]
    public void OpenProfile()
    {
        UnregisterEvents();
        _navigationService.NavigateToProfile();
    }

    [RelayCommand]
    public void OpenLeaderboard()
    {
        UnregisterEvents();
        _navigationService.NavigateToLeaderboard();
    }

    [RelayCommand]
    public async Task Logout()
    {
        await _networkService.LogoutAsync();
        _navigationService.NavigateToAuth(returnToMenu: false);
    }

    [RelayCommand]
    public async Task CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(RoomCode))
        {
            GenerateNewRoomCode();
        }

        IsError = false;
        StatusMessage = "Đang kết nối máy chủ & tạo phòng...";
        _networkService.ServerUrl = ServerUrl.Trim();

        try
        {
            bool success = await _networkService.CreateRoomAsync(RoomCode.Trim().ToUpperInvariant(), SelectedRule, TurnTimeLimit, IsRanked);
            if (success)
            {
                IsHosting = true;
                StatusMessage = $"Đang chờ người chơi vào phòng {RoomCode.ToUpperInvariant()}...";
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Không thể kết nối máy chủ: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(JoinCode))
        {
            IsError = true;
            StatusMessage = "Vui lòng nhập mã phòng muốn tham gia!";
            return;
        }

        IsError = false;
        IsJoining = true;
        StatusMessage = $"Đang tìm kiếm & kết nối vào phòng {JoinCode.Trim().ToUpperInvariant()}...";
        _networkService.ServerUrl = ServerUrl.Trim();

        try
        {
            await _networkService.JoinRoomAsync(JoinCode.Trim().ToUpperInvariant());
        }
        catch (Exception ex)
        {
            IsJoining = false;
            IsError = true;
            StatusMessage = $"Không thể tham gia phòng: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CancelHost()
    {
        await _networkService.LeaveRoomAsync();
        IsHosting = false;
        StatusMessage = "Đã hủy phòng. Bạn có thể tạo phòng mới hoặc tham gia phòng khác.";
    }

    [RelayCommand]
    public void CopyRoomCode()
    {
        try
        {
            Clipboard.SetText(RoomCode);
            StatusMessage = $"Đã sao chép mã {RoomCode} vào bộ nhớ tạm!";
        }
        catch
        {
            // Clipboard error handling
        }
    }

    [RelayCommand]
    public async Task BackToMenu()
    {
        UnregisterEvents();

        if (IsHosting)
        {
            await _networkService.LeaveRoomAsync();
        }

        _navigationService.NavigateToMenu();
    }
}
