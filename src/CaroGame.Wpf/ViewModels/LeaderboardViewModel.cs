using System.Collections.ObjectModel;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public int EloRating { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int TotalMatches => Wins + Losses + Draws;
    public double WinRate => TotalMatches > 0 ? Math.Round((double)Wins / TotalMatches * 100, 1) : 0;
    public bool IsCurrentUser { get; set; }

    public string CountryFlag => Country switch { "VN" => "🇻🇳", "JP" => "🇯🇵", "KR" => "🇰🇷", "US" => "🇺🇸", "GB" => "🇬🇧", "FR" => "🇫🇷", "DE" => "🇩🇪", _ => "🌐" };
    public string AvatarIcon => Avatar switch { "knight" => "⚔️", "ninja" => "🥷", "bot" => "🤖", "fox" => "🦊", "cat" => "🐱", "dragon" => "🐉", "lightning" => "⚡", "shield" => "🛡️", "star" => "🌟", _ => "👑" };

    public string RankDisplay => Rank switch
    {
        1 => "🥇 #1",
        2 => "🥈 #2",
        3 => "🥉 #3",
        _ => $"#{Rank}"
    };
}

public partial class LeaderboardViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;
    private readonly ISoundService _soundService;

    public ObservableCollection<LeaderboardEntry> TopPlayers { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _myRank = -1;

    public UserProfileDto? CurrentUser => _networkService.CurrentUser;
    public bool IsLoggedIn => CurrentUser != null;
    public string MyRankText => MyRank > 0 ? $"Thứ hạng của bạn: #{MyRank}" : "Bạn chưa nằm trong bảng xếp hạng";

    public LeaderboardViewModel(
        INavigationService navigationService,
        INetworkService networkService,
        ISoundService soundService)
    {
        _navigationService = navigationService;
        _networkService = networkService;
        _soundService = soundService;

        _ = LoadLeaderboardAsync();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await LoadLeaderboardAsync();
    }

    public async Task LoadLeaderboardAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusMessage = "Đang tải dữ liệu bảng xếp hạng từ máy chủ...";

        try
        {
            var list = await _networkService.GetLeaderboardAsync();
            TopPlayers.Clear();

            int rank = 1;
            int foundMyRank = -1;
            var currentUserId = CurrentUser?.Id;

            foreach (var user in list)
            {
                bool isMe = currentUserId.HasValue && user.Id == currentUserId.Value;
                if (isMe)
                {
                    foundMyRank = rank;
                }

                TopPlayers.Add(new LeaderboardEntry
                {
                    Rank = rank++,
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Username = user.Username,
                    Avatar = user.Avatar,
                    Country = user.Country,
                    EloRating = user.EloRating,
                    Wins = user.Wins,
                    Losses = user.Losses,
                    Draws = user.Draws,
                    IsCurrentUser = isMe
                });
            }

            MyRank = foundMyRank;
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(MyRankText));

            if (TopPlayers.Count == 0)
            {
                StatusMessage = "Chưa có người chơi nào trên bảng xếp hạng.";
            }
            else
            {
                StatusMessage = $"Cập nhật lúc {DateTime.Now:HH:mm:ss} • Top {TopPlayers.Count} cao thủ";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Không thể tải bảng xếp hạng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ViewProfile(LeaderboardEntry entry)
    {
        if (entry == null) return;
        _navigationService.NavigateToProfile(entry.Id);
    }

    [RelayCommand]
    public void Back()
    {
        _navigationService.NavigateToOnlineLobby();
    }

    [RelayCommand]
    public void BackToMenu()
    {
        _navigationService.NavigateToMenu();
    }
}
