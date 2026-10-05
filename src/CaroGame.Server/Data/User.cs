namespace CaroGame.Server.Data;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Player"; // "Player", "VIP", "Admin"
    public string Status { get; set; } = "Active"; // "Active", "Banned"
    public bool IsEmailVerified { get; set; } = true;
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public string Title { get; set; } = "Tân Thủ";
    public string AvatarFrame { get; set; } = "classic"; // "classic", "bronze", "silver", "gold", "diamond", "challenger"
    public int EloRating { get; set; } = 1000;
    public int PeakElo { get; set; } = 1000;
    public int LowestElo { get; set; } = 1000;
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public string Bio { get; set; } = "Đam mê cờ Caro!";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int WinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public long TotalPlayTimeSeconds { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public string? LastLoginIp { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
}
