namespace CaroGame.Server.Data;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int EloRating { get; set; } = 1000;
    public int PeakElo { get; set; } = 1000;
    public string Avatar { get; set; } = "king";
    public string Country { get; set; } = "VN";
    public string Bio { get; set; } = "Đam mê cờ Caro!";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int WinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
