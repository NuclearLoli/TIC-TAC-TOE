using System.ComponentModel.DataAnnotations;

namespace CaroGame.Data.Entities;

public class GameEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    public string GameMode { get; set; } = "PvC";

    public string? AiDifficulty { get; set; }

    public string RuleType { get; set; } = "BlockedBothEnds";

    public string Winner { get; set; } = "None"; // "X", "O", "Draw"

    public int DurationSeconds { get; set; }

    public int TotalMoves { get; set; }

    public List<MoveEntity> Moves { get; set; } = new();
}

public class MoveEntity
{
    [Key]
    public int Id { get; set; }

    public int GameId { get; set; }
    public GameEntity? Game { get; set; }

    public int TurnNumber { get; set; }

    public string Player { get; set; } = "X";

    public int Row { get; set; }

    public int Col { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
