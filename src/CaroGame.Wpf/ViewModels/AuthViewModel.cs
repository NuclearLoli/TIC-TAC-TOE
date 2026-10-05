using System.Windows.Threading;
using CaroGame.Core.Network;
using CaroGame.Wpf.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaroGame.Wpf.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INetworkService _networkService;
    private readonly ISoundService _soundService;
    private DispatcherTimer? _countdownTimer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isLoginMode = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isForgotPasswordMode = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isOtpStep = false;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _otpCode = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _resendSecondsLeft = 0;

    public bool CanResendOtp => ResendSecondsLeft <= 0 && !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackButtonText))]
    private bool _returnToMenu;

    public string Title
    {
        get
        {
            if (IsForgotPasswordMode)
                return IsOtpStep ? "XÁC NHẬN MẬT KHẨU MỚI" : "KHÔI PHỤC MẬT KHẨU";
            if (IsLoginMode)
                return "ĐĂNG NHẬP TÀI KHOẢN";
            return IsOtpStep ? "XÁC THỰC EMAIL (OTP)" : "ĐĂNG KÝ TÀI KHOẢN";
        }
    }

    public string SwitchModeText => IsLoginMode ? "Chưa có tài khoản? Đăng ký ngay" : "Đã có tài khoản? Đăng nhập";
    public string SubmitButtonText
    {
        get
        {
            if (IsLoginMode) return "ĐĂNG NHẬP";
            return IsOtpStep ? "XÁC NHẬN & TẠO TÀI KHOẢN" : "TIẾP TỤC & GỬI MÃ OTP ✉️";
        }
    }

    public string BackButtonText => ReturnToMenu ? "← Quay lại Menu chính" : "← Quay lại Sảnh Online";
    public string UsernameLabel => IsLoginMode ? "TÊN ĐĂNG NHẬP HOẶC EMAIL" : "TÊN TÀI KHOẢN (USERNAME)";

    public AuthViewModel(
        INavigationService navigationService,
        INetworkService networkService,
        ISoundService soundService)
    {
        _navigationService = navigationService;
        _networkService = networkService;
        _soundService = soundService;
    }

    [RelayCommand]
    public void SwitchMode()
    {
        IsLoginMode = !IsLoginMode;
        IsForgotPasswordMode = false;
        IsOtpStep = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        OtpCode = string.Empty;

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SwitchModeText));
        OnPropertyChanged(nameof(SubmitButtonText));
        OnPropertyChanged(nameof(UsernameLabel));
    }

    [RelayCommand]
    public void OpenForgotPassword()
    {
        IsForgotPasswordMode = true;
        IsLoginMode = false;
        IsOtpStep = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        OtpCode = string.Empty;
        NewPassword = string.Empty;

        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    public void BackToLogin()
    {
        IsForgotPasswordMode = false;
        IsLoginMode = true;
        IsOtpStep = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        OtpCode = string.Empty;

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SwitchModeText));
        OnPropertyChanged(nameof(SubmitButtonText));
        OnPropertyChanged(nameof(UsernameLabel));
    }

    [RelayCommand]
    public void BackFromOtp()
    {
        IsOtpStep = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        OtpCode = string.Empty;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SubmitButtonText));
    }

    [RelayCommand]
    public async Task Submit()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (IsLoginMode)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập (hoặc Email) và mật khẩu.";
                return;
            }

            IsLoading = true;
            try
            {
                var result = await _networkService.LoginAsync(Username.Trim(), Password);
                if (result.Success)
                {
                    _soundService.Play(Core.Enums.SoundEffectType.GameWon);
                    NavigateAfterSuccess();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể kết nối máy chủ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
            return;
        }

        // Register flow
        if (!IsOtpStep)
        {
            // Step 1: Validate input and send OTP email
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tên tài khoản và mật khẩu.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
            {
                ErrorMessage = "Vui lòng nhập địa chỉ Email hợp lệ (ví dụ: name@gmail.com).";
                return;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.";
                return;
            }

            IsLoading = true;
            try
            {
                var otpResult = await _networkService.SendOtpAsync(Email.Trim(), "Register");
                if (otpResult.Success)
                {
                    IsOtpStep = true;
                    SuccessMessage = otpResult.Message;
                    StartResendCountdown();
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(SubmitButtonText));
                }
                else
                {
                    ErrorMessage = otpResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi gửi mã OTP: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        else
        {
            // Step 2: Confirm OTP and create account
            if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Trim().Length < 4)
            {
                ErrorMessage = "Vui lòng nhập mã OTP xác thực (6 số) đã gửi vào email.";
                return;
            }

            IsLoading = true;
            try
            {
                string dispName = string.IsNullOrWhiteSpace(DisplayName) ? Username.Trim() : DisplayName.Trim();
                var result = await _networkService.RegisterAsync(Username.Trim(), Email.Trim(), Password, dispName, OtpCode.Trim());
                if (result.Success)
                {
                    _soundService.Play(Core.Enums.SoundEffectType.GameWon);
                    NavigateAfterSuccess();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng ký tài khoản: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task SendForgotPasswordOtp()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
        {
            ErrorMessage = "Vui lòng nhập địa chỉ Email đã đăng ký.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _networkService.SendOtpAsync(Email.Trim(), "ResetPassword");
            if (result.Success)
            {
                IsOtpStep = true;
                SuccessMessage = result.Message;
                StartResendCountdown();
                OnPropertyChanged(nameof(Title));
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi kết nối máy chủ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmResetPassword()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Trim().Length < 4)
        {
            ErrorMessage = "Vui lòng nhập mã xác thực OTP 6 số.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _networkService.ResetPasswordAsync(Email.Trim(), OtpCode.Trim(), NewPassword);
            if (result.Success)
            {
                _soundService.Play(Core.Enums.SoundEffectType.GameWon);
                IsForgotPasswordMode = false;
                IsLoginMode = true;
                IsOtpStep = false;
                SuccessMessage = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
                Password = string.Empty;
                NewPassword = string.Empty;
                OtpCode = string.Empty;
                OnPropertyChanged(nameof(Title));
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi đặt lại mật khẩu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResendOtp()
    {
        if (!CanResendOtp || string.IsNullOrWhiteSpace(Email)) return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsLoading = true;

        try
        {
            string purpose = IsForgotPasswordMode ? "ResetPassword" : "Register";
            var result = await _networkService.SendOtpAsync(Email.Trim(), purpose);
            if (result.Success)
            {
                SuccessMessage = "Đã gửi lại mã OTP mới. Vui lòng kiểm tra email!";
                StartResendCountdown();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể gửi lại mã: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartResendCountdown()
    {
        ResendSecondsLeft = 60;
        OnPropertyChanged(nameof(CanResendOtp));

        _countdownTimer?.Stop();
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += (_, _) =>
        {
            ResendSecondsLeft--;
            OnPropertyChanged(nameof(CanResendOtp));
            if (ResendSecondsLeft <= 0)
            {
                _countdownTimer?.Stop();
            }
        };
        _countdownTimer.Start();
    }

    private void NavigateAfterSuccess()
    {
        if (ReturnToMenu)
            _navigationService.NavigateToMenu();
        else
            _navigationService.NavigateToOnlineLobby();
    }

    [RelayCommand]
    public void Back()
    {
        if (ReturnToMenu)
            _navigationService.NavigateToMenu();
        else
            _navigationService.NavigateToOnlineLobby();
    }

    [RelayCommand]
    public void ContinueAsGuest()
    {
        _navigationService.NavigateToMenu();
    }
}
