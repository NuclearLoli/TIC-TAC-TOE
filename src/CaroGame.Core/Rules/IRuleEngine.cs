using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.Rules;

public readonly record struct WinResult(bool HasWon, CellState Winner, IReadOnlyList<Coordinate> WinningLine)
{
    public static WinResult None => new(false, CellState.Empty, Array.Empty<Coordinate>());
}

public interface IRuleEngine
{
    RuleType RuleType { get; }
    WinResult CheckWin(DynamicBoard board, Coordinate lastMove);
}
