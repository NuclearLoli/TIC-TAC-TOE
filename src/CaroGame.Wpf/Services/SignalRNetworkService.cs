using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace CaroGame.Wpf.Services;

public class SignalRNetworkService : INetworkService
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient = new();
    private readonly string _sessionFilePath;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? CurrentRoomCode { get; private set; }
    public CellState MyRole { get; private set; } = CellState.Empty;
    public string ServerUrl { get; set; } = "http://localhost:5000/carohub";
    public UserProfileDto? CurrentUser { get; private set; }
    public string? AuthToken { get; private set; }
    public GameStartDto? CurrentGameInfo { get; private set; }

    public event Action<string>? RoomCreated;
    public event Action<GameStartDto>? GameStarted;
    public event Action<NetworkMoveDto>? MoveReceived;
    public event Action<ChatMessageDto>? ChatMessageReceived;
    public event Action<MatchFinishDto>? MatchFinished;
    public event Action<string>? OpponentLeft;
    public event Action? RematchRequested;
    public event Action<GameStartDto>? RematchStarted;
    public event Action<string>? ErrorOccurred;
    public event Action<bool>? ConnectionStatusChanged;
    public event Action<UserProfileDto?>? UserProfileChanged;
    public event Action<ChallengeFriendDto>? FriendChallengeReceived;
    public event Action? FriendsListUpdated;

    private string ApiBaseUrl
    {
        get
        {
            var uri = new Uri(ServerUrl);
            return $"{uri.Scheme}://{uri.Authority}";
        }
    }

    public SignalRNetworkService()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroGame");
        Directory.CreateDirectory(folder);
        _sessionFilePath = Path.Combine(folder, "session.json");

        TryLoadLocalSession();
    }

    private void Dispatch(Action action)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    private void TryLoadLocalSession()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                string json = File.ReadAllText(_sessionFilePath);
                var session = JsonSerializer.Deserialize<AuthResponseDto>(json);
                if (session?.Token != null && session.User != null)
                {
                    AuthToken = session.Token;
                    CurrentUser = session.User;
                }
            }
        }
        catch
        {
            // Ignore corrupted session
        }
    }

    private void SaveLocalSession(AuthResponseDto auth)
    {
        try
        {
            string json = JsonSerializer.Serialize(auth);
            File.WriteAllText(_sessionFilePath, json);
        }
        catch
        {
            // Ignore save error
        }
    }

    private async Task EnsureServerRunningAsync()
    {
        try
        {
            var uri = new Uri(ServerUrl);
            if (!uri.IsLoopback) return;

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(400));
            var resp = await _httpClient.GetAsync($"{ApiBaseUrl}/health", cts.Token);
            if (resp.IsSuccessStatusCode) return;
        }
        catch
        {
            TryStartLocalServer();
            for (int i = 0; i < 8; i++)
            {
                await Task.Delay(400);
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                    var check = await _httpClient.GetAsync($"{ApiBaseUrl}/health", cts.Token);
                    if (check.IsSuccessStatusCode) return;
                }
                catch { }
            }
        }
    }

    private static void TryStartLocalServer()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            var candidates = new List<string>
            {
                Path.Combine(baseDir, "CaroGame.Server.exe"),
                Path.Combine(baseDir, "CaroServer", "CaroGame.Server.exe"),
                Path.Combine(baseDir, "..", "CaroServer", "CaroGame.Server.exe")
            };

            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                candidates.Add(Path.Combine(dir.FullName, "publish", "CaroServer", "CaroGame.Server.exe"));
                candidates.Add(Path.Combine(dir.FullName, "src", "CaroGame.Server", "bin", "Debug", "net9.0", "CaroGame.Server.exe"));
                candidates.Add(Path.Combine(dir.FullName, "src", "CaroGame.Server", "bin", "Release", "net9.0", "CaroGame.Server.exe"));
                candidates.Add(Path.Combine(dir.FullName, "CaroGame.Server", "bin", "Debug", "net9.0", "CaroGame.Server.exe"));
                candidates.Add(Path.Combine(dir.FullName, "CaroGame.Server", "bin", "Release", "net9.0", "CaroGame.Server.exe"));
                candidates.Add(Path.Combine(dir.FullName, "CaroServer", "CaroGame.Server.exe"));
                dir = dir.Parent;
            }

            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    var fullPath = Path.GetFullPath(p);
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        WorkingDirectory = Path.GetDirectoryName(fullPath) ?? baseDir,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi);
                    break;
                }
            }
        }
        catch
        {
            // Ignore if unable to start process automatically
        }
    }

    public Task<AuthResponseDto> RegisterAsync(string username, string password, string displayName)
        => RegisterAsync(username, $"{username.Trim()}@carogame.local", password, displayName, "");

    public async Task<AuthResponseDto> RegisterAsync(string username, string email, string password, string displayName, string otpCode = "")
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/register", new RegisterRequestDto
            {
                Username = username,
                Email = email,
                Password = password,
                DisplayName = displayName,
                OtpCode = otpCode
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result != null && result.Success && result.User != null)
            {
                AuthToken = result.Token;
                CurrentUser = result.User;
                SaveLocalSession(result);
                Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
                return result;
            }

            return result ?? new AuthResponseDto { Success = false, Message = "Đăng ký không thành công." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<CheckAvailabilityResponseDto> CheckAvailabilityAsync(string identifier)
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/check-availability", new CheckAvailabilityRequestDto
            {
                Identifier = identifier
            });

            var result = await response.Content.ReadFromJsonAsync<CheckAvailabilityResponseDto>();
            return result ?? new CheckAvailabilityResponseDto { Exists = false, Available = false, Message = "Phản hồi không hợp lệ từ máy chủ." };
        }
        catch (Exception ex)
        {
            return new CheckAvailabilityResponseDto { Exists = false, Available = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> SendOtpAsync(string email, string purpose = "Register")
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/send-otp", new SendOtpRequestDto
            {
                Email = email,
                Purpose = purpose
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Không thể gửi mã OTP." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(string email, string code, string purpose = "Register")
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/verify-otp", new VerifyOtpRequestDto
            {
                Email = email,
                Code = code,
                Purpose = purpose
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Xác thực OTP thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/reset-password", new ResetPasswordRequestDto
            {
                Email = email,
                OtpCode = otpCode,
                NewPassword = newPassword
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Đặt lại mật khẩu thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(string usernameOrEmail, string password)
    {
        await EnsureServerRunningAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/login", new LoginRequestDto
            {
                UsernameOrEmail = usernameOrEmail,
                Username = usernameOrEmail,
                Password = password
            });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result != null && result.Success && result.User != null)
            {
                AuthToken = result.Token;
                CurrentUser = result.User;
                SaveLocalSession(result);
                Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
                return result;
            }

            return result ?? new AuthResponseDto { Success = false, Message = "Đăng nhập không thành công." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public Task LogoutAsync()
    {
        AuthToken = null;
        CurrentUser = null;
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }
        catch { }

        Dispatch(() => UserProfileChanged?.Invoke(null));
        return Task.CompletedTask;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid? userId = null)
    {
        try
        {
            string url = userId.HasValue
                ? $"{ApiBaseUrl}/api/profile/{userId.Value}"
                : $"{ApiBaseUrl}/api/auth/profile";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(AuthToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                if (profile != null)
                {
                    if (!userId.HasValue || (CurrentUser != null && profile.Id == CurrentUser.Id))
                    {
                        CurrentUser = profile;
                        Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
                    }
                    return profile;
                }
            }
        }
        catch { }
        return userId.HasValue ? null : CurrentUser;
    }

    public async Task<AuthResponseDto> UpdateProfileAsync(UpdateProfileRequestDto req)
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            return new AuthResponseDto { Success = false, Message = "Chưa đăng nhập." };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/profile/update")
            {
                Content = JsonContent.Create(req)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result != null && result.Success && result.User != null)
            {
                CurrentUser = result.User;
                SaveLocalSession(new AuthResponseDto { Success = true, Token = AuthToken, User = CurrentUser });
                Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
            }
            return result ?? new AuthResponseDto { Success = false, Message = "Không thể cập nhật hồ sơ." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            return new AuthResponseDto { Success = false, Message = "Chưa đăng nhập." };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/profile/change-password")
            {
                Content = JsonContent.Create(new ChangePasswordRequestDto
                {
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Không thể đổi mật khẩu." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi kết nối máy chủ: {ex.Message}" };
        }
    }

    public async Task<List<FriendDto>> GetFriendsAsync()
    {
        if (string.IsNullOrEmpty(AuthToken)) return new List<FriendDto>();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/api/friends");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<FriendDto>>();
                return list ?? new List<FriendDto>();
            }
        }
        catch { }
        return new List<FriendDto>();
    }

    public async Task<AuthResponseDto> SendFriendRequestAsync(string targetUsername)
    {
        if (string.IsNullOrEmpty(AuthToken)) return new AuthResponseDto { Success = false, Message = "Chưa đăng nhập." };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/friends/request")
            {
                Content = JsonContent.Create(new SendFriendRequestDto { TargetUsername = targetUsername })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Gửi lời mời thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> RespondFriendRequestAsync(Guid friendshipId, bool accept)
    {
        if (string.IsNullOrEmpty(AuthToken)) return new AuthResponseDto { Success = false, Message = "Chưa đăng nhập." };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/friends/respond")
            {
                Content = JsonContent.Create(new FriendRequestActionDto { FriendshipId = friendshipId, Accept = accept })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Xử lý thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
        }
    }

    public async Task<AuthResponseDto> RemoveFriendAsync(Guid friendId)
    {
        if (string.IsNullOrEmpty(AuthToken)) return new AuthResponseDto { Success = false, Message = "Chưa đăng nhập." };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiBaseUrl}/api/friends/{friendId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? new AuthResponseDto { Success = false, Message = "Xóa bạn bè thất bại." };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Message = $"Lỗi: {ex.Message}" };
        }
    }

    public async Task<List<UserProfileDto>> SearchUsersAsync(string query)
    {
        if (string.IsNullOrEmpty(AuthToken)) return new List<UserProfileDto>();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/api/users/search?q={Uri.EscapeDataString(query)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<UserProfileDto>>();
                return list ?? new List<UserProfileDto>();
            }
        }
        catch { }
        return new List<UserProfileDto>();
    }

    public async Task ChallengeFriendAsync(Guid friendId, string roomCode)
    {
        if (_hubConnection != null && IsConnected)
        {
            await _hubConnection.InvokeAsync("ChallengeFriend", friendId, roomCode);
        }
    }

    public async Task<List<UserProfileDto>> GetLeaderboardAsync()
    {
        await EnsureServerRunningAsync();
        try
        {
            var list = await _httpClient.GetFromJsonAsync<List<UserProfileDto>>($"{ApiBaseUrl}/api/leaderboard");
            return list ?? new List<UserProfileDto>();
        }
        catch
        {
            return new List<UserProfileDto>();
        }
    }

    public async Task ConnectAsync()
    {
        await EnsureServerRunningAsync();
        if (IsConnected && _hubConnection != null)
        {
            return;
        }

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(ServerUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
            .Build();

        _hubConnection.On<string>("RoomCreated", (roomCode) =>
        {
            CurrentRoomCode = roomCode;
            Dispatch(() => RoomCreated?.Invoke(roomCode));
        });

        _hubConnection.On<GameStartDto>("GameStarted", (dto) =>
        {
            CurrentGameInfo = dto;
            CurrentRoomCode = dto.RoomCode;
            MyRole = dto.YourRole;
            Dispatch(() => GameStarted?.Invoke(dto));
        });

        _hubConnection.On<NetworkMoveDto>("MoveReceived", (dto) =>
        {
            Dispatch(() => MoveReceived?.Invoke(dto));
        });

        _hubConnection.On<ChatMessageDto>("ChatMessageReceived", (dto) =>
        {
            dto.IsMe = CurrentUser != null && dto.SenderName == CurrentUser.DisplayName;
            Dispatch(() => ChatMessageReceived?.Invoke(dto));
        });

        _hubConnection.On<MatchFinishDto>("MatchFinished", (dto) =>
        {
            if (CurrentUser != null)
            {
                if (MyRole == CellState.X)
                {
                    CurrentUser.EloRating = dto.NewEloX;
                }
                else if (MyRole == CellState.O)
                {
                    CurrentUser.EloRating = dto.NewEloO;
                }

                if (dto.Winner == MyRole) CurrentUser.Wins++;
                else if (dto.Winner != CellState.Empty) CurrentUser.Losses++;
                else CurrentUser.Draws++;

                Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
            }
            Dispatch(() => MatchFinished?.Invoke(dto));
        });

        _hubConnection.On<string>("OpponentLeft", (reason) =>
        {
            Dispatch(() => OpponentLeft?.Invoke(reason));
        });

        _hubConnection.On("RematchRequested", () =>
        {
            Dispatch(() => RematchRequested?.Invoke());
        });

        _hubConnection.On<GameStartDto>("RematchStarted", (dto) =>
        {
            CurrentGameInfo = dto;
            MyRole = dto.YourRole;
            Dispatch(() => RematchStarted?.Invoke(dto));
        });

        _hubConnection.On<string>("ErrorOccurred", (err) =>
        {
            Dispatch(() => ErrorOccurred?.Invoke(err));
        });

        _hubConnection.On<UserProfileDto>("Authenticated", (profile) =>
        {
            CurrentUser = profile;
            Dispatch(() => UserProfileChanged?.Invoke(CurrentUser));
        });

        _hubConnection.On<ChallengeFriendDto>("ReceiveFriendChallenge", (dto) =>
        {
            Dispatch(() => FriendChallengeReceived?.Invoke(dto));
        });

        _hubConnection.On("FriendsListUpdated", () =>
        {
            Dispatch(() => FriendsListUpdated?.Invoke());
        });

        _hubConnection.Reconnecting += _ =>
        {
            Dispatch(() => ConnectionStatusChanged?.Invoke(false));
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async _ =>
        {
            Dispatch(() => ConnectionStatusChanged?.Invoke(true));
            if (!string.IsNullOrEmpty(AuthToken) && _hubConnection != null)
            {
                await _hubConnection.InvokeAsync("Authenticate", AuthToken);
            }
        };

        _hubConnection.Closed += _ =>
        {
            Dispatch(() => ConnectionStatusChanged?.Invoke(false));
            return Task.CompletedTask;
        };

        await _hubConnection.StartAsync();

        if (!string.IsNullOrEmpty(AuthToken))
        {
            await _hubConnection.InvokeAsync("Authenticate", AuthToken);
        }

        Dispatch(() => ConnectionStatusChanged?.Invoke(true));
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            CurrentRoomCode = null;
            CurrentGameInfo = null;
            MyRole = CellState.Empty;
            Dispatch(() => ConnectionStatusChanged?.Invoke(false));
        }
    }

    public async Task<bool> CreateRoomAsync(string roomCode, RuleType rule, int turnTime, bool isRanked = true)
    {
        await ConnectAsync();
        if (_hubConnection == null || !IsConnected) return false;

        CurrentRoomCode = roomCode;
        MyRole = CellState.X;
        await _hubConnection.InvokeAsync("CreateRoom", roomCode, (int)rule, turnTime, isRanked);
        return true;
    }

    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        await ConnectAsync();
        if (_hubConnection == null || !IsConnected) return false;

        CurrentRoomCode = roomCode;
        MyRole = CellState.O;
        await _hubConnection.InvokeAsync("JoinRoom", roomCode);
        return true;
    }

    public async Task SendMoveAsync(Coordinate coord, CellState player, int turnNumber)
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode))
        {
            await _hubConnection.InvokeAsync("SendMove", CurrentRoomCode, coord.Row, coord.Col, (int)player, turnNumber);
        }
    }

    public async Task SendChatMessageAsync(string message)
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode) && !string.IsNullOrWhiteSpace(message))
        {
            await _hubConnection.InvokeAsync("SendChatMessage", CurrentRoomCode, message.Trim());
        }
    }

    public async Task ReportGameOverAsync(CellState winner, string reason)
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode))
        {
            await _hubConnection.InvokeAsync("ReportGameOver", CurrentRoomCode, (int)winner, reason);
        }
    }

    public async Task RequestRematchAsync()
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode))
        {
            await _hubConnection.InvokeAsync("RequestRematch", CurrentRoomCode);
        }
    }

    public async Task AcceptRematchAsync()
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode))
        {
            await _hubConnection.InvokeAsync("AcceptRematch", CurrentRoomCode);
        }
    }

    public async Task LeaveRoomAsync()
    {
        if (_hubConnection != null && IsConnected && !string.IsNullOrEmpty(CurrentRoomCode))
        {
            try
            {
                await _hubConnection.InvokeAsync("LeaveRoom", CurrentRoomCode);
            }
            catch
            {
                // Ignore if disconnected
            }
        }
        CurrentRoomCode = null;
        CurrentGameInfo = null;
        MyRole = CellState.Empty;
    }
}
