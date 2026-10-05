using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class MenuViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;
    private readonly GameSettings _settings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPvC))]
    [NotifyPropertyChangedFor(nameof(IsPvP))]
    private GameMode _selectedMode = GameMode.PvC;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEasy))]
    [NotifyPropertyChangedFor(nameof(IsMedium))]
    [NotifyPropertyChangedFor(nameof(IsHard))]
    private AiDifficulty _selectedDifficulty = AiDifficulty.Medium;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVietnameseRule))]
    [NotifyPropertyChangedFor(nameof(IsFreeRule))]
    private RuleType _selectedRule = RuleType.BlockedBothEnds;

    [ObservableProperty]
    private bool _enableTimer = true;

    [ObservableProperty]
    private int _turnTimeSeconds = 30;

    // Chess.com style User Identity
    public UserProfileDto? CurrentUser => _networkService.CurrentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public string UserDisplayName => CurrentUser?.DisplayName ?? "Khách";
    public string UserEloText => CurrentUser != null ? $"⭐ {CurrentUser.EloRating} Elo" : "1000 Elo";
    public string UserStatsText => CurrentUser != null
        ? $"{CurrentUser.Wins}W - {CurrentUser.Losses}L - {CurrentUser.Draws}D  •  Tỉ lệ: {CurrentUser.WinRate}%"
        : "Đăng nhập tài khoản để tích lũy điểm Elo & leo Rank";
    public string UserInitial => !string.IsNullOrEmpty(CurrentUser?.DisplayName)
        ? CurrentUser.DisplayName[0].ToString().ToUpperInvariant()
        : "👤";
    public string UserAvatarIcon => CurrentUser?.AvatarIcon ?? "👑";
    public string UserFlag => CurrentUser?.CountryFlag ?? "🇻🇳";

    public bool IsPvC
    {
        get => SelectedMode == GameMode.PvC;
        set { if (value) SelectedMode = GameMode.PvC; }
    }

    public bool IsPvP
    {
        get => SelectedMode == GameMode.PvP;
        set { if (value) SelectedMode = GameMode.PvP; }
    }

    public bool IsEasy
    {
        get => SelectedDifficulty == AiDifficulty.Easy;
        set { if (value) SelectedDifficulty = AiDifficulty.Easy; }
    }

    public bool IsMedium
    {
        get => SelectedDifficulty == AiDifficulty.Medium;
        set { if (value) SelectedDifficulty = AiDifficulty.Medium; }
    }

    public bool IsHard
    {
        get => SelectedDifficulty == AiDifficulty.Hard;
        set { if (value) SelectedDifficulty = AiDifficulty.Hard; }
    }

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

    public MenuViewModel(
        INavigationService navigationService,
        INetworkService networkService,
        GameSettings settings)
    {
        _navigationService = navigationService;
        _networkService = networkService;
        _settings = settings;

        SelectedMode = settings.Mode;
        SelectedDifficulty = settings.Difficulty;
        SelectedRule = settings.Rule;
        EnableTimer = settings.EnableTimer;
        TurnTimeSeconds = settings.TurnTimeLimitSeconds;

        _networkService.UserProfileChanged += OnUserProfileChanged;
    }

    private void OnUserProfileChanged(UserProfileDto? profile)
    {
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(UserDisplayName));
        OnPropertyChanged(nameof(UserEloText));
        OnPropertyChanged(nameof(UserStatsText));
        OnPropertyChanged(nameof(UserInitial));
        OnPropertyChanged(nameof(UserAvatarIcon));
        OnPropertyChanged(nameof(UserFlag));
    }

    [RelayCommand]
    private void StartGame()
    {
        _settings.Mode = SelectedMode;
        _settings.Difficulty = SelectedDifficulty;
        _settings.Rule = SelectedRule;
        _settings.EnableTimer = EnableTimer;
        _settings.TurnTimeLimitSeconds = TurnTimeSeconds;

        _navigationService.NavigateToGamePlay();
    }

    [RelayCommand]
    private void StartPvC()
    {
        SelectedMode = GameMode.PvC;
        StartGame();
    }

    [RelayCommand]
    private void StartPvP()
    {
        SelectedMode = GameMode.PvP;
        StartGame();
    }

    [RelayCommand]
    private void OpenOnlineLobby()
    {
        _navigationService.NavigateToOnlineLobby();
    }

    [RelayCommand]
    private void OpenProfile()
    {
        _navigationService.NavigateToProfile();
    }

    [RelayCommand]
    private void OpenFriends()
    {
        _navigationService.NavigateToFriends();
    }

    [RelayCommand]
    private void OpenLeaderboard()
    {
        _navigationService.NavigateToLeaderboard();
    }

    [RelayCommand]
    private void OpenLogin()
    {
        _navigationService.NavigateToAuth(returnToMenu: true, isRegister: false);
    }

    [RelayCommand]
    private void OpenRegister()
    {
        _navigationService.NavigateToAuth(returnToMenu: true, isRegister: true);
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _networkService.LogoutAsync();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService.NavigateToSettings();
    }

    [RelayCommand]
    private void OpenHistory()
    {
        _navigationService.NavigateToHistory();
    }
}
