using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.Network;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class SendOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Register"; // "Register" or "ResetPassword"
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Register";
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Username { get => UsernameOrEmail; set => UsernameOrEmail = value; }
    public string Password { get; set; } = string.Empty;
}

public class CheckAvailabilityRequestDto
{
    public string Identifier { get; set; } = string.Empty;
}

public class CheckAvailabilityResponseDto
{
    public bool Exists { get; set; }
    public bool IsEmail { get; set; }
    public bool Available { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SuggestedUsername { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Player"; // "Player", "VIP", "Admin"
    public string Status { get; set; } = "Active";
    public bool IsEmailVerified { get; set; } = true;
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public string Title { get; set; } = "Tân Thủ";
    public string AvatarFrame { get; set; } = "classic"; // "classic", "bronze", "silver", "gold", "diamond", "challenger"
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public string Bio { get; set; } = "Đam mê cờ Caro!";
    public int EloRating { get; set; } = 1000;
    public int PeakElo { get; set; } = 1000;
    public int LowestElo { get; set; } = 1000;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int WinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public long TotalPlayTimeSeconds { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TotalMatches => Wins + Losses + Draws;
    public double WinRate => TotalMatches > 0 ? Math.Round((double)Wins / TotalMatches * 100, 1) : 0;

    public int CurrentLevelXp => ExperiencePoints % 100;
    public int NextLevelXp => 100;
    public double XpProgressPercentage => Math.Min(100.0, Math.Max(0.0, (CurrentLevelXp / 100.0) * 100.0));

    public string FormattedPlayTime
    {
        get
        {
            var ts = TimeSpan.FromSeconds(TotalPlayTimeSeconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }

    public string AvatarFrameBorderBrush => AvatarFrame switch
    {
        "bronze" => "#CD7F32",
        "silver" => "#C0C0C0",
        "gold" => "#FFD700",
        "diamond" => "#00E5FF",
        "challenger" => "#FF4655",
        _ => "#81B64C" // classic
    };

    public string RankTier => EloRating switch
    {
        >= 1800 => "👑 Đại Kiện Tướng",
        >= 1500 => "💎 Kiện Tướng",
        >= 1300 => "🥇 Tinh Anh",
        >= 1100 => "🥈 Nghiệp Dư",
        _ => "🥉 Tập Sự"
    };

    public string CountryFlag => Country switch
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

    public bool HasCustomAvatar => !string.IsNullOrEmpty(Avatar) && (Avatar.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || Avatar.StartsWith("http", StringComparison.OrdinalIgnoreCase) || Avatar.Length > 40);

    public string AvatarIcon => HasCustomAvatar ? "👤" : (Avatar switch
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
}

public class UpdateProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public string Bio { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? AvatarFrame { get; set; }
}

public class ChangePasswordRequestDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class LinkEmailRequestDto
{
    public string NewEmail { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class FriendDto
{
    public Guid FriendshipId { get; set; }
    public Guid FriendId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public string Bio { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1000;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public bool IsOnline { get; set; }
    public bool IsInGame { get; set; }
    public string Status { get; set; } = "Accepted"; // "Accepted", "Pending", "Sent"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string StatusText => IsInGame ? "🔴 Đang thi đấu" : (IsOnline ? "🟢 Online" : "⚫ Ngoại tuyến");
    public string CountryFlag => Country switch { "VN" => "🇻🇳", "JP" => "🇯🇵", "KR" => "🇰🇷", "US" => "🇺🇸", "GB" => "🇬🇧", "FR" => "🇫🇷", "DE" => "🇩🇪", _ => "🌐" };
    public bool HasCustomAvatar => !string.IsNullOrEmpty(Avatar) && (Avatar.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || Avatar.StartsWith("http", StringComparison.OrdinalIgnoreCase) || Avatar.Length > 40);
    public string AvatarIcon => HasCustomAvatar ? "👤" : (Avatar switch { "knight" => "⚔️", "ninja" => "🥷", "bot" => "🤖", "fox" => "🦊", "cat" => "🐱", "dragon" => "🐉", "lightning" => "⚡", "shield" => "🛡️", "star" => "🌟", _ => "👑" });
}

public class SendFriendRequestDto
{
    public string TargetUsername { get; set; } = string.Empty;
}

public class FriendRequestActionDto
{
    public Guid FriendshipId { get; set; }
    public bool Accept { get; set; }
}

public class ChallengeFriendDto
{
    public Guid ChallengerId { get; set; }
    public string ChallengerName { get; set; } = string.Empty;
    public string ChallengerAvatar { get; set; } = "king";
    public int ChallengerElo { get; set; } = 1000;
    public string RoomCode { get; set; } = string.Empty;

    public bool HasCustomAvatar => !string.IsNullOrEmpty(ChallengerAvatar) && (ChallengerAvatar.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || ChallengerAvatar.StartsWith("http", StringComparison.OrdinalIgnoreCase) || ChallengerAvatar.Length > 40);
    public string AvatarIcon => HasCustomAvatar ? "👤" : (ChallengerAvatar switch { "knight" => "⚔️", "ninja" => "🥷", "bot" => "🤖", "fox" => "🦊", "cat" => "🐱", "dragon" => "🐉", "lightning" => "⚡", "shield" => "🛡️", "star" => "🌟", _ => "👑" });
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserProfileDto? User { get; set; }
}

public class ChatMessageDto
{
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsMe { get; set; }
}

public class OnlineRoomConfigDto
{
    public RuleType Rule { get; set; } = RuleType.BlockedBothEnds;
    public int TurnTimeSeconds { get; set; } = 30;
    public bool IsRanked { get; set; } = true;
}

public class GameStartDto
{
    public string RoomCode { get; set; } = string.Empty;
    public RuleType Rule { get; set; } = RuleType.BlockedBothEnds;
    public int TurnTimeLimitSeconds { get; set; } = 30;
    public bool IsRanked { get; set; } = true;
    public CellState YourRole { get; set; } = CellState.X;
    public CellState FirstPlayer { get; set; } = CellState.X;

    public string HostName { get; set; } = "Chủ phòng";
    public int HostElo { get; set; } = 1000;
    public string HostAvatar { get; set; } = "king";

    public string GuestName { get; set; } = "Khách";
    public int GuestElo { get; set; } = 1000;
    public string GuestAvatar { get; set; } = "knight";

    public string OpponentName(CellState myRole) => myRole == CellState.X ? GuestName : HostName;
    public int OpponentElo(CellState myRole) => myRole == CellState.X ? GuestElo : HostElo;
    public string OpponentAvatar(CellState myRole) => myRole == CellState.X ? GuestAvatar : HostAvatar;
}

public class NetworkMoveDto
{
    public string RoomCode { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Col { get; set; }
    public CellState Player { get; set; }
    public int TurnNumber { get; set; }

    public Coordinate ToCoordinate() => new(Row, Col);
}

public class MatchFinishDto
{
    public string RoomCode { get; set; } = string.Empty;
    public CellState Winner { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public int EloChangeX { get; set; }
    public int EloChangeO { get; set; }
    public int NewEloX { get; set; }
    public int NewEloO { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RoomInfoDto
{
    public string RoomCode { get; set; } = string.Empty;
    public string HostConnectionId { get; set; } = string.Empty;
    public string? GuestConnectionId { get; set; }
    public RuleType Rule { get; set; } = RuleType.BlockedBothEnds;
    public int TurnTimeSeconds { get; set; } = 30;
    public bool IsRanked { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
