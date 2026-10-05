namespace CaroGame.Core.Enums;

public enum GameMode
{
    PvP,
    PvC,
    Online,
    Replay
}

public enum AiDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum RuleType
{
    FreeRule,
    BlockedBothEnds
}

public enum GameState
{
    NotStarted,
    Playing,
    Paused,
    WonX,
    WonO,
    Draw
}

public enum SoundEffectType
{
    MovePlaced,
    WarningFour,
    GameWon,
    GameLost,
    UndoMove,
    TimerTick,
    ChatMessage
}
