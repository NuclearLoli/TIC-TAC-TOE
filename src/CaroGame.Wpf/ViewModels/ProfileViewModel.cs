using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;
    private readonly Guid? _targetUserId;

    [ObservableProperty]
    private UserProfileDto? _profile;

    [ObservableProperty]
    private bool _isMyProfile = true;

    [ObservableProperty]
    private string _title = "HỒ SƠ KỲ THỦ";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverviewTab))]
    [NotifyPropertyChangedFor(nameof(IsEditTab))]
    [NotifyPropertyChangedFor(nameof(IsSecurityTab))]
    private string _currentTab = "Overview"; // Overview, Edit, Security

    public bool IsOverviewTab => CurrentTab == "Overview";
    public bool IsEditTab => CurrentTab == "Edit";
    public bool IsSecurityTab => CurrentTab == "Security";

    // Edit fields
    [ObservableProperty]
    private string _editDisplayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomAvatar))]
    [NotifyPropertyChangedFor(nameof(PreviewAvatarIcon))]
    private string _editAvatar = "king";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewCountryFlag))]
    private string _editCountry = "VN";

    [ObservableProperty]
    private string _editBio = string.Empty;

    [ObservableProperty]
    private string _editTitle = "Tân Thủ";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewAvatarFrameBorderBrush))]
    private string _editAvatarFrame = "classic";

    public string PreviewAvatarFrameBorderBrush => EditAvatarFrame switch
    {
        "bronze" => "#CD7F32",
        "silver" => "#C0C0C0",
        "gold" => "#FFD700",
        "diamond" => "#00E5FF",
        "challenger" => "#FF4655",
        _ => "#81B64C"
    };

    public string PreviewCountryFlag => EditCountry switch
    {
        "VN" => "🇻🇳",
        "JP" => "🇯🇵",
        "KR" => "🇰🇷",
        "US" => "🇺🇸",
        "GB" => "🇬🇧",
        "FR" => "🇫🇷",
        "DE" => "🇩🇪",
        _ => "🌐"
    };

    public string PreviewAvatarIcon => HasCustomAvatar ? "👤" : (EditAvatar switch
    {
        "knight" => "⚔️",
        "ninja" => "🥷",
        "bot" => "🤖",
        "fox" => "🦊",
        "cat" => "🐱",
        "dragon" => "🐉",
        "lightning" => "⚡",
        "shield" => "🛡️",
        "star" => "🌟",
        _ => "👑"
    });

    // Security fields
    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    // Feedback
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string _successMessage = string.Empty;

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    [ObservableProperty]
    private bool _isBusy;

    public ProfileViewModel(
        INavigationService navigationService,
        INetworkService networkService,
        Guid? targetUserId = null)
    {
        _navigationService = navigationService;
        _networkService = networkService;
        _targetUserId = targetUserId;

        LoadProfile();
    }

    public async void LoadProfile()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;

        var myUser = _networkService.CurrentUser;
        IsMyProfile = !_targetUserId.HasValue || (_targetUserId.HasValue && myUser != null && _targetUserId.Value == myUser.Id);
        Title = IsMyProfile ? "HỒ SƠ CỦA BẠN" : "HỒ SƠ KỲ THỦ";

        var p = await _networkService.GetProfileAsync(_targetUserId);
        if (p != null)
        {
            Profile = p;
            EditDisplayName = p.DisplayName;
            EditAvatar = p.Avatar;
            EditCountry = p.Country;
            EditBio = p.Bio;
            EditTitle = p.Title;
            EditAvatarFrame = p.AvatarFrame;
        }
        else if (myUser != null && IsMyProfile)
        {
            Profile = myUser;
            EditDisplayName = myUser.DisplayName;
            EditAvatar = myUser.Avatar;
            EditCountry = myUser.Country;
            EditBio = myUser.Bio;
            EditTitle = myUser.Title;
            EditAvatarFrame = myUser.AvatarFrame;
        }

        IsBusy = false;
    }

    [RelayCommand]
    public void SwitchTab(string tab)
    {
        CurrentTab = tab;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;
        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsEditTab));
        OnPropertyChanged(nameof(IsSecurityTab));
    }

    public bool HasCustomAvatar => !string.IsNullOrEmpty(EditAvatar) && (EditAvatar.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || EditAvatar.StartsWith("http", StringComparison.OrdinalIgnoreCase) || EditAvatar.Length > 40);

    [RelayCommand]
    public void SelectAvatar(string avatarKey)
    {
        EditAvatar = avatarKey;
    }

    [RelayCommand]
    public void UploadCustomAvatar()
    {
        string? dataUri = CaroGame.Wpf.Helpers.ImageHelper.PickAndProcessAvatar(targetSize: 256, quality: 85);
        if (!string.IsNullOrEmpty(dataUri))
        {
            EditAvatar = dataUri;
            SuccessMessage = "Đã chọn ảnh! Nhấn 'Lưu Thay Đổi' để áp dụng ảnh đại diện mới.";
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    public void RemoveCustomAvatar()
    {
        EditAvatar = "king";
        SuccessMessage = "Đã chuyển về icon mặc định. Nhấn 'Lưu Thay Đổi' để áp dụng.";
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public void SelectAvatarFrame(string frame)
    {
        EditAvatarFrame = frame;
    }

    [RelayCommand]
    public void SelectTitle(string title)
    {
        EditTitle = title;
    }

    [RelayCommand]
    public void SelectCountry(string countryCode)
    {
        EditCountry = countryCode;
    }

    [RelayCommand]
    public async Task SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(EditDisplayName) || EditDisplayName.Trim().Length < 2)
        {
            StatusMessage = "Tên hiển thị phải có ít nhất 2 ký tự.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;

        var res = await _networkService.UpdateProfileAsync(new UpdateProfileRequestDto
        {
            DisplayName = EditDisplayName.Trim(),
            Avatar = EditAvatar,
            Country = EditCountry,
            Bio = EditBio?.Trim() ?? string.Empty,
            Title = EditTitle,
            AvatarFrame = EditAvatarFrame
        });

        IsBusy = false;
        if (res.Success && res.User != null)
        {
            Profile = res.User;
            SuccessMessage = "✅ Đã lưu cập nhật hồ sơ thành công!";
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task ChangePassword()
    {
        if (string.IsNullOrEmpty(OldPassword))
        {
            StatusMessage = "Vui lòng nhập mật khẩu hiện tại.";
            return;
        }
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
        {
            StatusMessage = "Mật khẩu mới phải có tối thiểu 6 ký tự.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "Xác nhận mật khẩu mới không trùng khớp.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        SuccessMessage = string.Empty;

        var res = await _networkService.ChangePasswordAsync(OldPassword, NewPassword);
        IsBusy = false;

        if (res.Success)
        {
            SuccessMessage = "✅ Đổi mật khẩu thành công!";
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        else
        {
            StatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task AddFriend()
    {
        if (Profile == null) return;
        IsBusy = true;
        var res = await _networkService.SendFriendRequestAsync(Profile.Username);
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
    public void OpenFriends()
    {
        _navigationService.NavigateToFriends();
    }

    [RelayCommand]
    public void OpenSecuritySettings()
    {
        _navigationService.NavigateToSettings("Security");
    }

    [RelayCommand]
    public void Back()
    {
        _navigationService.NavigateToMenu();
    }
}
