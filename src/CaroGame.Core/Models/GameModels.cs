using CaroGame.Core.Enums;

namespace CaroGame.Core.Models;

public record MoveRecord(int TurnNumber, CellState Player, Coordinate Coord, DateTime Timestamp);

public class GameSettings
{
    public RuleType Rule { get; set; } = RuleType.BlockedBothEnds;
    public AiDifficulty Difficulty { get; set; } = AiDifficulty.Medium;
    public GameMode Mode { get; set; } = GameMode.PvC;
    public int InitialHalfSize { get; set; } = 7; // 15x15 initial
    public int TurnTimeLimitSeconds { get; set; } = 30;
    public bool EnableTimer { get; set; } = true;
    public bool EnableSound { get; set; } = true;
    public double SoundVolume { get; set; } = 0.8;
}

public class AudioProfile
{
    public Dictionary<SoundEffectType, string> CustomFilePaths { get; set; } = new();
    public Dictionary<SoundEffectType, bool> EnabledSounds { get; set; } = new()
    {
        [SoundEffectType.MovePlaced] = true,
        [SoundEffectType.WarningFour] = true,
        [SoundEffectType.GameWon] = true,
        [SoundEffectType.GameLost] = true,
        [SoundEffectType.UndoMove] = true,
        [SoundEffectType.TimerTick] = true
    };
    public double MasterVolume { get; set; } = 0.8;
}
