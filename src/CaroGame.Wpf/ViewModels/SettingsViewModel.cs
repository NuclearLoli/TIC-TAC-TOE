using System.Collections.ObjectModel;
using System.IO;
using CaroGame.Core.Enums;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private double _masterVolume;

    public ObservableCollection<SoundItemViewModel> SoundItems { get; } = new();

    public SettingsViewModel(
        INavigationService navigationService,
        ISoundService soundService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _soundService = soundService;
        _dialogService = dialogService;

        _masterVolume = _soundService.Profile.MasterVolume;

        InitializeSoundItems();
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
