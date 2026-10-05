using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.Network;

public interface INetworkService
{
    bool IsConnected { get; }
    string? CurrentRoomCode { get; }
    CellState MyRole { get; }
    string ServerUrl { get; set; }
    UserProfileDto? CurrentUser { get; }
    string? AuthToken { get; }
    GameStartDto? CurrentGameInfo { get; }

    event Action<string>? RoomCreated;
    event Action<GameStartDto>? GameStarted;
    event Action<NetworkMoveDto>? MoveReceived;
    event Action<ChatMessageDto>? ChatMessageReceived;
    event Action<MatchFinishDto>? MatchFinished;
    event Action<string>? OpponentLeft;
    event Action? RematchRequested;
    event Action<GameStartDto>? RematchStarted;
    event Action<string>? ErrorOccurred;
    event Action<bool>? ConnectionStatusChanged;
    event Action<UserProfileDto?>? UserProfileChanged;
    event Action<ChallengeFriendDto>? FriendChallengeReceived;
    event Action? FriendsListUpdated;

    Task ConnectAsync();
    Task DisconnectAsync();
    Task<AuthResponseDto> SendOtpAsync(string email, string purpose = "Register");
    Task<AuthResponseDto> VerifyOtpAsync(string email, string code, string purpose = "Register");
    Task<AuthResponseDto> ResetPasswordAsync(string email, string otpCode, string newPassword);
    Task<AuthResponseDto> RegisterAsync(string username, string email, string password, string displayName, string otpCode = "");
    Task<AuthResponseDto> RegisterAsync(string username, string password, string displayName) => RegisterAsync(username, $"{username.Trim()}@carogame.local", password, displayName, "");
    Task<AuthResponseDto> LoginAsync(string usernameOrEmail, string password);
    Task LogoutAsync();
    Task<UserProfileDto?> GetProfileAsync(Guid? userId = null);
    Task<AuthResponseDto> UpdateProfileAsync(UpdateProfileRequestDto req);
    Task<AuthResponseDto> ChangePasswordAsync(string oldPassword, string newPassword);
    Task<List<FriendDto>> GetFriendsAsync();
    Task<AuthResponseDto> SendFriendRequestAsync(string targetUsername);
    Task<AuthResponseDto> RespondFriendRequestAsync(Guid friendshipId, bool accept);
    Task<AuthResponseDto> RemoveFriendAsync(Guid friendId);
    Task<List<UserProfileDto>> SearchUsersAsync(string query);
    Task ChallengeFriendAsync(Guid friendId, string roomCode);
    Task<List<UserProfileDto>> GetLeaderboardAsync();

    Task<bool> CreateRoomAsync(string roomCode, RuleType rule, int turnTime, bool isRanked = true);
    Task<bool> JoinRoomAsync(string roomCode);
    Task SendMoveAsync(Coordinate coord, CellState player, int turnNumber);
    Task SendChatMessageAsync(string message);
    Task ReportGameOverAsync(CellState winner, string reason);
    Task RequestRematchAsync();
    Task AcceptRematchAsync();
    Task LeaveRoomAsync();
}
