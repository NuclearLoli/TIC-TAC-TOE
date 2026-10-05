using System.Collections.ObjectModel;
using System.Windows;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class FriendsViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;

    [ObservableProperty]
    private string _currentTab = "Friends"; // Friends, Requests, Search

    public bool IsFriendsTab => CurrentTab == "Friends";
    public bool IsRequestsTab => CurrentTab == "Requests";
    public bool IsSearchTab => CurrentTab == "Search";

    [ObservableProperty]
    private ObservableCollection<FriendDto> _acceptedFriends = new();

    [ObservableProperty]
    private ObservableCollection<FriendDto> _pendingRequests = new();

    [ObservableProperty]
    private ObservableCollection<UserProfileDto> _searchResults = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _pendingRequestsCount;

    [ObservableProperty]
    private int _onlineFriendsCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    // Challenge Dialog State
    [ObservableProperty]
    private bool _hasIncomingChallenge;

    [ObservableProperty]
    private ChallengeFriendDto? _incomingChallenge;

    public FriendsViewModel(
        INavigationService navigationService,
        INetworkService networkService)
    {
        _navigationService = navigationService;
        _networkService = networkService;

        _networkService.FriendChallengeReceived += OnFriendChallengeReceived;
        _networkService.FriendsListUpdated += OnFriendsListUpdated;

        _ = RefreshFriends();
    }

    private void OnFriendsListUpdated()
    {
        _ = RefreshFriends();
    }

    private void OnFriendChallengeReceived(ChallengeFriendDto challenge)
    {
        IncomingChallenge = challenge;
        HasIncomingChallenge = true;
    }

    [RelayCommand]
    public async Task AcceptChallenge()
    {
        if (IncomingChallenge == null) return;
        string room = IncomingChallenge.RoomCode;
        HasIncomingChallenge = false;
        IncomingChallenge = null;

        await _networkService.JoinRoomAsync(room);
    }

    [RelayCommand]
    public void DeclineChallenge()
    {
        HasIncomingChallenge = false;
        IncomingChallenge = null;
    }

    [RelayCommand]
    public void SwitchTab(string tab)
    {
        CurrentTab = tab;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;
        OnPropertyChanged(nameof(IsFriendsTab));
        OnPropertyChanged(nameof(IsRequestsTab));
        OnPropertyChanged(nameof(IsSearchTab));
    }

    [RelayCommand]
    public async Task RefreshFriends()
    {
        IsBusy = true;
        StatusMessage = string.Empty;

        var allFriends = await _networkService.GetFriendsAsync();

        AcceptedFriends.Clear();
        PendingRequests.Clear();

        foreach (var f in allFriends)
        {
            if (f.Status == "Accepted")
            {
                AcceptedFriends.Add(f);
            }
            else if (f.Status == "Pending")
            {
                PendingRequests.Add(f);
            }
        }

        PendingRequestsCount = PendingRequests.Count;
        OnlineFriendsCount = AcceptedFriends.Count(f => f.IsOnline);

        IsBusy = false;
    }

    [RelayCommand]
    public async Task AcceptRequest(FriendDto friend)
    {
        if (friend == null) return;
        IsBusy = true;
        var res = await _networkService.RespondFriendRequestAsync(friend.FriendshipId, true);
        IsBusy = false;

        if (res.Success)
        {
            SuccessMessage = $"Đã kết bạn với {friend.DisplayName}!";
            await RefreshFriends();
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task DeclineRequest(FriendDto friend)
    {
        if (friend == null) return;
        IsBusy = true;
        var res = await _networkService.RespondFriendRequestAsync(friend.FriendshipId, false);
        IsBusy = false;

        if (res.Success)
        {
            SuccessMessage = "Đã từ chối lời mời kết bạn.";
            await RefreshFriends();
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task RemoveFriend(FriendDto friend)
    {
        if (friend == null) return;
        var confirm = MessageBox.Show($"Bạn có chắc chắn muốn hủy kết bạn với {friend.DisplayName}?", "Xác nhận hủy kết bạn", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        var res = await _networkService.RemoveFriendAsync(friend.FriendId);
        IsBusy = false;

        if (res.Success)
        {
            SuccessMessage = $"Đã hủy kết bạn với {friend.DisplayName}.";
            await RefreshFriends();
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task SearchUsers()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Trim().Length < 2)
        {
            StatusMessage = "Vui lòng nhập tối thiểu 2 ký tự để tìm kiếm.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;

        var list = await _networkService.SearchUsersAsync(SearchQuery.Trim());
        SearchResults.Clear();
        foreach (var u in list)
        {
            SearchResults.Add(u);
        }

        if (SearchResults.Count == 0)
        {
            StatusMessage = "Không tìm thấy người chơi nào phù hợp.";
        }

        IsBusy = false;
    }

    [RelayCommand]
    public async Task SendFriendRequest(UserProfileDto user)
    {
        if (user == null) return;
        IsBusy = true;
        var res = await _networkService.SendFriendRequestAsync(user.Username);
        IsBusy = false;

        if (res.Success)
        {
            SuccessMessage = res.Message;
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task ChallengeFriend(FriendDto friend)
    {
        if (friend == null) return;
        if (!friend.IsOnline)
        {
            StatusMessage = $"{friend.DisplayName} hiện đang ngoại tuyến.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Đang tạo phòng và gửi lời thách đấu tới {friend.DisplayName}...";

        // Generate challenge room code
        string code = "CHAL-" + Random.Shared.Next(1000, 9999);
        bool created = await _networkService.CreateRoomAsync(code, Core.Enums.RuleType.BlockedBothEnds, 30, true);
        if (created)
        {
            await _networkService.ChallengeFriendAsync(friend.FriendId, code);
            SuccessMessage = $"Đã gửi lời thách đấu tới {friend.DisplayName}! Đang chờ đối thủ chấp nhận...";
            _navigationService.NavigateToOnlineLobby();
        }
        else
        {
            StatusMessage = "Không thể tạo phòng thách đấu.";
        }

        IsBusy = false;
    }

    [RelayCommand]
    public void ViewProfile(FriendDto friend)
    {
        if (friend == null) return;
        _navigationService.NavigateToProfile(friend.FriendId);
    }

    [RelayCommand]
    public void ViewSearchUserProfile(UserProfileDto user)
    {
        if (user == null) return;
        _navigationService.NavigateToProfile(user.Id);
    }

    [RelayCommand]
    public void Back()
    {
        _navigationService.NavigateToMenu();
    }
}
