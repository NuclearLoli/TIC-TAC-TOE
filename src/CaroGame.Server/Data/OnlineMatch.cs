using CaroGame.Core.Enums;

namespace CaroGame.Server.Data;

public class OnlineMatch
{
    public int Id { get; set; }
    public Guid PlayerXId { get; set; }
    public Guid PlayerOId { get; set; }
    public Guid? WinnerId { get; set; }
    public int EloChangeX { get; set; }
    public int EloChangeO { get; set; }
    public RuleType Rule { get; set; }
    public bool IsRanked { get; set; } = true;
    public int TotalMoves { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
