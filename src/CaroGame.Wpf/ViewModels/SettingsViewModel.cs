using System.Collections.ObjectModel;
using System.IO;
using CaroGame.Core.Enums;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;
    private readonly INetworkService _networkService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSoundTab))]
    [NotifyPropertyChangedFor(nameof(IsSecurityTab))]
    private string _currentTab = "Sound"; // "Sound", "Security"

    public bool IsSoundTab => CurrentTab == "Sound";
    public bool IsSecurityTab => CurrentTab == "Security";

    // Sound settings
    [ObservableProperty]
    private double _masterVolume;

    public ObservableCollection<SoundItemViewModel> SoundItems { get; } = new();

    // Security - Account info
    [ObservableProperty]
    private UserProfileDto? _currentUser;

    [ObservableProperty]
    private string _currentEmail = string.Empty;

    [ObservableProperty]
    private bool _isEmailVerified;

    // Security - Change Password
    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordStatus))]
    private string _passwordStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordSuccess))]
    private string _passwordSuccessMessage = string.Empty;

    public bool HasPasswordStatus => !string.IsNullOrEmpty(PasswordStatusMessage);
    public bool HasPasswordSuccess => !string.IsNullOrEmpty(PasswordSuccessMessage);

    [ObservableProperty]
    private bool _isPasswordBusy = false;

    // Security - Link Email
    [ObservableProperty]
    private string _newEmail = string.Empty;

    [ObservableProperty]
    private string _emailOtpCode = string.Empty;

    [ObservableProperty]
    private bool _isEmailOtpSent = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailStatus))]
    private string _emailStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailSuccess))]
    private string _emailSuccessMessage = string.Empty;

    public bool HasEmailStatus => !string.IsNullOrEmpty(EmailStatusMessage);
    public bool HasEmailSuccess => !string.IsNullOrEmpty(EmailSuccessMessage);

    [ObservableProperty]
    private bool _isEmailBusy = false;

    public SettingsViewModel(
        INavigationService navigationService,
        ISoundService soundService,
        IDialogService dialogService,
        INetworkService networkService)
    {
        _navigationService = navigationService;
        _soundService = soundService;
        _dialogService = dialogService;
        _networkService = networkService;

        _masterVolume = _soundService.Profile.MasterVolume;

        InitializeSoundItems();
        RefreshAccountInfo();
    }

    public void RefreshAccountInfo()
    {
        CurrentUser = _networkService.CurrentUser;
        CurrentEmail = !string.IsNullOrWhiteSpace(CurrentUser?.Email) ? CurrentUser.Email : "Chưa liên kết";
        IsEmailVerified = CurrentUser?.IsEmailVerified ?? false;
    }

    [RelayCommand]
    public void SwitchTab(string tab)
    {
        CurrentTab = tab;
        if (tab == "Security")
        {
            RefreshAccountInfo();
        }
    }

    private void InitializeSoundItems()
    {
        SoundItems.Clear();

        AddSoundItem(SoundEffectType.MovePlaced, "Tiếng đánh cờ (Move Placed)");
        AddSoundItem(SoundEffectType.WarningFour, "Cảnh báo 4 ô (Warning 4)");
        AddSoundItem(SoundEffectType.GameWon, "Âm thanh chiến thắng (Game Won)");
        AddSoundItem(SoundEffectType.GameLost, "Âm thanh thua trận (Game Lost)");
        AddSoundItem(SoundEffectType.UndoMove, "Âm thanh rút lại nước (Undo)");
        AddSoundItem(SoundEffectType.TimerTick, "Âm thanh đếm ngược (Timer Tick)");
    }

    private void AddSoundItem(SoundEffectType type, string displayName)
    {
        bool isEnabled = _soundService.Profile.EnabledSounds.GetValueOrDefault(type, true);
        string currentPath = _soundService.Profile.CustomFilePaths.GetValueOrDefault(type, "Mặc định hệ thống");

        var item = new SoundItemViewModel(type, displayName, isEnabled, currentPath, _soundService, _dialogService);
        SoundItems.Add(item);
    }

    partial void OnMasterVolumeChanged(double value)
    {
        _soundService.SetVolume(value);
    }

    [RelayCommand]
    private void ResetAllToDefault()
    {
        if (_dialogService.ShowConfirmation("Xác nhận", "Bạn có muốn khôi phục toàn bộ cài đặt âm thanh về mặc định?"))
        {
            foreach (var item in SoundItems)
            {
                item.ResetToDefault();
            }
            MasterVolume = 0.8;
            _soundService.SaveSettings();
        }
    }

    [RelayCommand]
    public async Task ChangePassword()
    {
        PasswordStatusMessage = string.Empty;
        PasswordSuccessMessage = string.Empty;

        if (string.IsNullOrEmpty(OldPassword))
        {
            PasswordStatusMessage = "Vui lòng nhập mật khẩu hiện tại.";
            return;
        }
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 6)
        {
            PasswordStatusMessage = "Mật khẩu mới phải có tối thiểu 6 ký tự.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            PasswordStatusMessage = "Xác nhận mật khẩu mới không trùng khớp.";
            return;
        }

        IsPasswordBusy = true;
        var res = await _networkService.ChangePasswordAsync(OldPassword, NewPassword);
        IsPasswordBusy = false;

        if (res.Success)
        {
            PasswordSuccessMessage = "✅ Đổi mật khẩu thành công!";
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        else
        {
            PasswordStatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task SendLinkEmailOtp()
    {
        EmailStatusMessage = string.Empty;
        EmailSuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewEmail) || !NewEmail.Contains('@') || !NewEmail.Contains('.'))
        {
            EmailStatusMessage = "Vui lòng nhập địa chỉ Email mới hợp lệ.";
            return;
        }

        if (string.Equals(NewEmail.Trim(), CurrentEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            EmailStatusMessage = "Email này trùng với email hiện tại của tài khoản.";
            return;
        }

        IsEmailBusy = true;
        var res = await _networkService.SendOtpAsync(NewEmail.Trim(), "LinkEmail");
        IsEmailBusy = false;

        if (res.Success)
        {
            IsEmailOtpSent = true;
            EmailSuccessMessage = $"✅ Mã OTP 6 số đã được gửi tới {NewEmail.Trim()}. Vui lòng kiểm tra hòm thư!";
        }
        else
        {
            EmailStatusMessage = res.Message;
        }
    }

    [RelayCommand]
    public async Task ConfirmLinkEmail()
    {
        EmailStatusMessage = string.Empty;
        EmailSuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewEmail))
        {
            EmailStatusMessage = "Vui lòng nhập địa chỉ Email.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailOtpCode) || EmailOtpCode.Trim().Length != 6)
        {
            EmailStatusMessage = "Vui lòng nhập mã OTP 6 chữ số.";
            return;
        }

        IsEmailBusy = true;
        var res = await _networkService.LinkEmailAsync(NewEmail.Trim(), EmailOtpCode.Trim());
        IsEmailBusy = false;

        if (res.Success)
        {
            EmailSuccessMessage = "✅ Liên kết Email thành công!";
            RefreshAccountInfo();
            NewEmail = string.Empty;
            EmailOtpCode = string.Empty;
            IsEmailOtpSent = false;
        }
        else
        {
            EmailStatusMessage = res.Message;
        }
    }

    [RelayCommand]
    private void BackToMenu()
    {
        _soundService.SaveSettings();
        _navigationService.NavigateToMenu();
    }
}

public partial class SoundItemViewModel : ViewModelBase
{
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;

    public SoundEffectType SoundType { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _filePath;

    public SoundItemViewModel(
        SoundEffectType soundType,
        string displayName,
        bool isEnabled,
        string filePath,
        ISoundService soundService,
        IDialogService dialogService)
    {
        SoundType = soundType;
        DisplayName = displayName;
        _isEnabled = isEnabled;
        _filePath = filePath;
        _soundService = soundService;
        _dialogService = dialogService;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _soundService.SetSoundEnabled(SoundType, value);
    }

    [RelayCommand]
    public void BrowseFile()
    {
        string? selected = _dialogService.ShowOpenFileDialog(
            $"Chọn file âm thanh cho {DisplayName}",
            "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*");

        if (!string.IsNullOrEmpty(selected) && File.Exists(selected))
        {
            _soundService.SetCustomFile(SoundType, selected);
            FilePath = selected;
            _soundService.Play(SoundType); // Test ngay
        }
    }

    [RelayCommand]
    public void ResetToDefault()
    {
        _soundService.ResetToDefault(SoundType);
        FilePath = "Mặc định hệ thống";
        IsEnabled = true;
    }

    [RelayCommand]
    public void TestSound()
    {
        _soundService.Play(SoundType);
    }
}
