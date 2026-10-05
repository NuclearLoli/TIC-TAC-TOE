using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INetworkService _networkService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    public UserProfileDto? CurrentUser => _networkService.CurrentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public string UserDisplayName => !string.IsNullOrWhiteSpace(CurrentUser?.DisplayName)
        ? CurrentUser.DisplayName
        : (!string.IsNullOrWhiteSpace(CurrentUser?.Username) ? CurrentUser.Username : "Khách");
    public string UserEloText => CurrentUser != null ? $"⭐ {CurrentUser.EloRating} Elo" : "";
    public string UserAvatarIcon => CurrentUser?.AvatarIcon ?? "👤";
    public string UserFlag => CurrentUser?.CountryFlag ?? "";

    public MainViewModel(INetworkService networkService, INavigationService navigationService)
    {
        _networkService = networkService;
        _navigationService = navigationService;
        _networkService.UserProfileChanged += OnUserProfileChanged;
    }

    private void OnUserProfileChanged(UserProfileDto? profile)
    {
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(UserDisplayName));
        OnPropertyChanged(nameof(UserEloText));
        OnPropertyChanged(nameof(UserAvatarIcon));
        OnPropertyChanged(nameof(UserFlag));
    }

    [RelayCommand]
    public void OpenProfileOrAuth()
    {
        if (IsLoggedIn)
        {
            _navigationService.NavigateToProfile();
        }
        else
        {
            _navigationService.NavigateToAuth(returnToMenu: true);
        }
    }

    [RelayCommand]
    public void OpenProfile()
    {
        _navigationService.NavigateToProfile();
    }

    [RelayCommand]
    public void OpenFriends()
    {
        _navigationService.NavigateToFriends();
    }

    [RelayCommand]
    public void OpenAuth()
    {
        _navigationService.NavigateToAuth(returnToMenu: true);
    }

    [RelayCommand]
    public async Task Logout()
    {
        await _networkService.LogoutAsync();
        _navigationService.NavigateToAuth(returnToMenu: false);
    }

    public void SetView(ViewModelBase view)
    {
        CurrentView = view;
    }
}
